namespace NFSLibrary
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using NFSLibrary.Protocols.Commons;
    using NFSLibrary.Protocols.V2;
    using NFSLibrary.Protocols.V3;
    using NFSLibrary.Protocols.V4;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using static System.String;

    /// <summary>
    /// NFS Client Library.
    /// </summary>
    public class NfsClient : INfsClient
    {
        #region Fields

        private NFSPermission? _Mode;
        private bool _IsMounted;
        private bool _IsConnected;
        private readonly string _CurrentDirectory = Empty;
        private bool _Disposed;

        private readonly INFS _NfsInterface;
        private readonly ILogger<NfsClient> _Logger;
        private readonly NfsVersion _Version;

        private int _BlockSize = 7900;

        #endregion Fields

        #region Events

        /// <summary>
        /// Delegate for handling NFS data transfer events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing transfer details.</param>
        public delegate void NfsDataEventHandler(object sender, NfsEventArgs e);

        /// <summary>
        /// This event is fired when data is transferred from/to the server.
        /// </summary>
        public event NfsDataEventHandler DataEvent;

        #endregion Events

        #region Properties

        /// <summary>
        /// This property tells if the current export is mounted.
        /// </summary>
        public bool IsMounted => _IsMounted;

        /// <summary>
        /// This property tells if the connection is active.
        /// </summary>
        public bool IsConnected => _IsConnected;

        /// <summary>
        /// This property allow you to set file/folder access permissions.
        /// </summary>
        public NFSPermission Mode
        {
            get => _Mode ??= new NFSPermission(7, 7, 7);
            set => _Mode = value;
        }

        /// <summary>
        /// This property contains the current server directory.
        /// </summary>
        public string CurrentDirectory => _CurrentDirectory;

        /// <summary>
        /// Gets the block size for read/write operations.
        /// Block size must not be greater than 8064 for V2 and 8000 for V3.
        /// RPC Buffer size is fixed to 8192; we reserve 128 bytes for header
        /// information of V2 and 192 bytes for header information of V3.
        /// </summary>
        public int BlockSize => _BlockSize;

        #endregion Properties

        #region Constructor

        /// <summary>
        /// NFS Client Constructor.
        /// </summary>
        /// <param name="version">The required NFS version.</param>
        public NfsClient(NfsVersion version)
            : this(version, null)
        {
        }

        /// <summary>
        /// NFS Client Constructor with logging support.
        /// </summary>
        /// <param name="version">The required NFS version.</param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        public NfsClient(NfsVersion version, ILogger<NfsClient>? logger)
        {
            _Version = version;
            _Logger = logger ?? NullLogger<NfsClient>.Instance;

            _Logger.LogDebug("Creating NFS client for version {Version}", version);

            switch (version)
            {
                case NfsVersion.V2:
                    _NfsInterface = new NFSv2();
                    break;

                case NfsVersion.V3:
                    _NfsInterface = new NFSv3();
                    break;

                case NfsVersion.V4:
                    _NfsInterface = new NFSv4();
                    break;

                default:
                    throw new NotImplementedException($"NFS version {version} is not implemented.");
            }

            _Logger.LogDebug("NFS client created successfully for version {Version}", version);
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        /// Create a connection to a NFS Server using default options.
        /// </summary>
        /// <param name="address">The server address.</param>
        public void Connect(IPAddress address)
        {
            Connect(address, NfsConnectionOptions.Default);
        }

        /// <summary>
        /// Create a connection to a NFS Server using the specified options.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="options">The connection options. If null, default options are used.</param>
        public void Connect(IPAddress address, NfsConnectionOptions? options)
        {
            options ??= NfsConnectionOptions.Default;
            Connect(
                address,
                options.UserId,
                options.GroupId,
                options.CommandTimeoutMs,
                options.CharacterEncoding,
                options.UseSecurePort,
                options.UseFileHandleCache,
                options.NfsPort,
                options.MountPort);
        }

        /// <summary>
        /// Create a connection to a NFS Server.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="userId">The unix user id.</param>
        /// <param name="groupId">The unix group id.</param>
        /// <param name="commandTimeout">The command timeout in milliseconds.</param>
        public void Connect(IPAddress address, int userId, int groupId, int commandTimeout)
        {
            Connect(address, userId, groupId, commandTimeout, System.Text.Encoding.ASCII, true, false, 0, 0);
        }

        /// <summary>
        /// Create a connection to a NFS Server.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="userId">The unix user id.</param>
        /// <param name="groupId">The unix group id.</param>
        /// <param name="commandTimeout">The command timeout in milliseconds.</param>
        /// <param name="characterEncoding">Connection encoding.</param>
        /// <param name="useSecurePort">Uses a local binding port less than 1024.</param>
        /// <param name="useCache">Whether to use file handle caching.</param>
        /// <param name="nfsPort">The NFS server port. Use 0 to discover via portmapper.</param>
        /// <param name="mountPort">The mount protocol port (NFSv2/v3 only). Use 0 to discover via portmapper.</param>
        public void Connect(IPAddress address, int userId, int groupId, int commandTimeout, System.Text.Encoding characterEncoding, bool useSecurePort, bool useCache, int nfsPort = 0, int mountPort = 0)
        {
            _Logger.LogInformation("Connecting to NFS server at {Address} (version {Version}, userId={UserId}, groupId={GroupId}, nfsPort={NfsPort}, mountPort={MountPort})",
                address, _Version, userId, groupId, nfsPort, mountPort);

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                _NfsInterface.Connect(address, userId, groupId, commandTimeout, characterEncoding, useSecurePort, useCache, nfsPort, mountPort);
                _IsConnected = true;

                sw.Stop();
                _Logger.LogInformation("Connected to NFS server at {Address} in {ElapsedMs}ms", address, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _Logger.LogError(ex, "Failed to connect to NFS server at {Address} after {ElapsedMs}ms", address, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Close the current connection.
        /// </summary>
        public void Disconnect()
        {
            _Logger.LogInformation("Disconnecting from NFS server");

            try
            {
                _NfsInterface.Disconnect();
                _IsConnected = false;
                _Logger.LogInformation("Disconnected from NFS server successfully");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Error during disconnect from NFS server");
                throw;
            }
        }

        /// <summary>
        /// Get the list of the exported NFS devices.
        /// </summary>
        /// <returns>A list of the exported NFS devices.</returns>
        public List<string> GetExportedDevices()
        {
            _Logger.LogDebug("Getting exported devices");
            List<string> devices = _NfsInterface.GetExportedDevices();
            _Logger.LogDebug("Found {Count} exported devices", devices.Count);
            return devices;
        }

        /// <summary>
        /// Mount device.
        /// </summary>
        /// <param name="deviceName">The device name.</param>
        public void MountDevice(string deviceName)
        {
            _Logger.LogInformation("Mounting device: {DeviceName}", deviceName);

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                _NfsInterface.MountDevice(deviceName);
                //cuz of NFS v4.1 we have to do this after session is created
                _BlockSize = _NfsInterface.GetBlockSize();
                _IsMounted = true;

                sw.Stop();
                _Logger.LogInformation("Mounted device {DeviceName} in {ElapsedMs}ms (blockSize={BlockSize})",
                    deviceName, sw.ElapsedMilliseconds, _BlockSize);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _Logger.LogError(ex, "Failed to mount device {DeviceName} after {ElapsedMs}ms", deviceName, sw.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Unmount the current device.
        /// </summary>
        public void UnMountDevice()
        {
            _Logger.LogInformation("Unmounting device");

            try
            {
                _NfsInterface.UnMountDevice();
                _IsMounted = false;
                _Logger.LogInformation("Device unmounted successfully");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Error during unmount");
                throw;
            }
        }

        /// <summary>
        /// Get the items in a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory name (e.g. "directory\subdirectory" or "." for the root).</param>
        /// <returns>A list of the items name.</returns>
        public List<string> GetItemList(string directoryFullName)
        {
            return GetItemList(directoryFullName, true);  //changed to true cuz i don't need .. and .
        }

        /// <summary>
        /// Get the items in a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory name (e.g. "directory\subdirectory" or "." for the root).</param>
        /// <param name="excludeNavigationDots">When posted as true, return list will not contains "." and "..".</param>
        /// <returns>A list of the items name.</returns>
        public List<string> GetItemList(string directoryFullName, bool excludeNavigationDots)
        {
            directoryFullName = CorrectPath(directoryFullName);

            List<string> content = _NfsInterface.GetItemList(directoryFullName);

            if (excludeNavigationDots)
            {
                int dotIdx = content.IndexOf(".");
                if (dotIdx > -1)
                    content.RemoveAt(dotIdx);

                int ddotIdx = content.IndexOf("..");
                if (ddotIdx > -1)
                    content.RemoveAt(ddotIdx);
            }

            return content;
        }

        /// <summary>
        /// Get an item attributes.
        /// </summary>
        /// <param name="itemFullName">The item full path name.</param>
        /// <param name="throwExceptionIfNotFoud">Whether to throw an exception if the item is not found.</param>
        /// <returns>A NFSAttributes class.</returns>
        public NFSAttributes GetItemAttributes(string itemFullName, bool throwExceptionIfNotFoud = true)
        {
            itemFullName = CorrectPath(itemFullName);

            return _NfsInterface.GetItemAttributes(itemFullName, throwExceptionIfNotFoud);
        }

        /// <summary>
        /// Create a new directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        public void CreateDirectory(string directoryFullName)
        {
            CreateDirectory(directoryFullName, _Mode);
        }

        /// <summary>
        /// Create a new directory with Permission.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        /// <param name="mode">Directory permissions.</param>
        public void CreateDirectory(string directoryFullName, NFSPermission mode)
        {
            directoryFullName = CorrectPath(directoryFullName);

            string parentPath = System.IO.Path.GetDirectoryName(directoryFullName);

            if (!IsNullOrEmpty(parentPath) &&
                CompareOrdinal(parentPath, ".") != 0 &&
                !FileExists(parentPath))
            {
                CreateDirectory(parentPath);
            }

            _NfsInterface.CreateDirectory(directoryFullName, mode);
        }

        /// <summary>
        /// Delete a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        public void DeleteDirectory(string directoryFullName)
        {
            DeleteDirectory(directoryFullName, true);
        }

        /// <summary>
        /// Delete a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        /// <param name="recursive">Whether to delete contents recursively.</param>
        public void DeleteDirectory(string directoryFullName, bool recursive)
        {
            directoryFullName = CorrectPath(directoryFullName);

            if (recursive)
            {
                foreach (string item in GetItemList(directoryFullName, true))
                {
                    if (IsDirectory($"{directoryFullName}\\{item}"))
                    { DeleteDirectory($"{directoryFullName}\\{item}", recursive); }
                    else
                    { DeleteFile($"{directoryFullName}\\{item}"); }
                }
            }

            _NfsInterface.DeleteDirectory(directoryFullName);
        }

        /// <summary>
        /// Delete a file.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        public void DeleteFile(string fileFullName)
        {
            fileFullName = CorrectPath(fileFullName);

            _NfsInterface.DeleteFile(fileFullName);
        }

        /// <summary>
        /// Create a new file.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        public void CreateFile(string fileFullName)
        {
            CreateFile(fileFullName, _Mode);
        }

        /// <summary>
        /// Create a new file with permission.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        /// <param name="mode">File permission.</param>
        public void CreateFile(string fileFullName, NFSPermission mode)
        {
            fileFullName = CorrectPath(fileFullName);

            _NfsInterface.CreateFile(fileFullName, mode);
        }

        /// <summary>
        /// Copy a set of files from a remote directory to a local directory.
        /// </summary>
        /// <param name="sourceFileNames">A list of the remote files name.</param>
        /// <param name="sourceDirectoryFullName">The remote directory path (e.g. "directory\sub1\sub2" or "." for the root).</param>
        /// <param name="destinationDirectoryFullName">The destination local directory.</param>
        public void Read(IEnumerable<string> sourceFileNames, string sourceDirectoryFullName, string destinationDirectoryFullName)
        {
            if (!System.IO.Directory.Exists(destinationDirectoryFullName))
                return;
            foreach (string fileName in sourceFileNames)
            {
                Read(Combine(fileName, sourceDirectoryFullName), System.IO.Path.Combine(destinationDirectoryFullName, fileName));
            }
        }

        /// <summary>
        /// Copy a file from a remote directory to a local directory.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file name.</param>
        /// <param name="destinationFileFullName">The destination local directory.</param>
        public void Read(string sourceFileFullName, string destinationFileFullName)
        {
            System.IO.Stream fs = null;
            try
            {
                if (System.IO.File.Exists(destinationFileFullName))
                    System.IO.File.Delete(destinationFileFullName);
                fs = new System.IO.FileStream(destinationFileFullName, System.IO.FileMode.CreateNew);
                Read(sourceFileFullName, ref fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Copy a file from a remote directory to a stream.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file name.</param>
        /// <param name="outputStream">The output stream to write to.</param>
        public void Read(string sourceFileFullName, ref System.IO.Stream outputStream)
        {
            Read(sourceFileFullName, ref outputStream, CancellationToken.None);
        }

        /// <summary>
        /// Copy a file from a remote directory to a stream with cancellation support.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file name.</param>
        /// <param name="outputStream">The output stream to write to.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        public void Read(string sourceFileFullName, ref System.IO.Stream outputStream, CancellationToken cancellationToken)
        {
            if (outputStream != null)
            {
                sourceFileFullName = CorrectPath(sourceFileFullName);

                if (!FileExists(sourceFileFullName))
                    throw new System.IO.FileNotFoundException();

                cancellationToken.ThrowIfCancellationRequested();

                NFSAttributes nfsAttributes = GetItemAttributes(sourceFileFullName, true);
                long totalRead = nfsAttributes.Size, readOffset = 0;

                _Logger.LogDebug("Reading file {FilePath} ({Size} bytes)", sourceFileFullName, totalRead);

                byte[] chunkBuffer = ArrayPool<byte>.Shared.Rent(BlockSize);
                try
                {
                    int readCount, readLength = BlockSize;

                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (totalRead < readLength)
                        {
                            readLength = (int)totalRead;
                        }

                        readCount = _NfsInterface.Read(sourceFileFullName, readOffset, readLength, ref chunkBuffer);

                        DataEvent?.Invoke(this, new NfsEventArgs(readCount));

                        outputStream.Write(chunkBuffer, 0, readCount);

                        totalRead -= readCount; readOffset += readCount;
                    }
                    while (readCount != 0);

                    outputStream.Flush();

                    CompleteIo();

                    _Logger.LogDebug("Completed reading file {FilePath}", sourceFileFullName);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(chunkBuffer);
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(outputStream), "Output stream must not be null.");
            }
        }

        /// <summary>
        /// Copy a remote file to a buffer, CompleteIO proc must called end of the reading process for system stability.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file full path.</param>
        /// <param name="offset">Start offset.</param>
        /// <param name="totalLenght">Number of bytes.</param>
        /// <param name="buffer">Output buffer.</param>
        /// <returns>The number of copied bytes.</returns>
        public long Read(string sourceFileFullName, long offset, long totalLenght, ref byte[] buffer)
        {
            /* This function is not suitable for large file reading.
             * Big file reading will cause OS paging creation and
             * huge memory consumption.
             */
            sourceFileFullName = CorrectPath(sourceFileFullName);

            long exactTotalLength = totalLenght, currentPosition = 0;

            /* Prepare full Buffer to read */
            buffer = new byte[exactTotalLength];

            byte[] chunkBuffer = ArrayPool<byte>.Shared.Rent(BlockSize);
            try
            {
                int readCount = 0, readLength = BlockSize;

                do
                {
                    if (exactTotalLength - currentPosition < readLength)
                        readLength = (int)(exactTotalLength - currentPosition);

                    readCount = _NfsInterface.Read(sourceFileFullName, offset + currentPosition, readLength, ref chunkBuffer);

                    DataEvent?.Invoke(this, new NfsEventArgs(readCount));

                    Array.Copy(chunkBuffer, 0, buffer, currentPosition, readCount);

                    currentPosition += readCount;
                }
                while (readCount != 0);

                return currentPosition;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkBuffer);
            }
        }

        /// <summary>
        /// Copy a remote file to a buffer.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file full path.</param>
        /// <param name="offset">Start offset.</param>
        /// <param name="totalLenght">Number of bytes.</param>
        /// <param name="buffer">Output buffer.</param>
        public void Read(string sourceFileFullName, long offset, ref long totalLenght, ref byte[] buffer)
        {
            sourceFileFullName = CorrectPath(sourceFileFullName);

            uint blockSize = (uint)this.BlockSize;
            uint currentPosition = 0;

            byte[] chunkBuffer = ArrayPool<byte>.Shared.Rent((int)blockSize);
            try
            {
                do
                {
                    uint chunkCount = blockSize;
                    if (totalLenght - currentPosition < blockSize)
                        chunkCount = (uint)totalLenght - currentPosition;

                    int size = _NfsInterface.Read(sourceFileFullName, offset + currentPosition, (int)chunkCount, ref chunkBuffer);

                    DataEvent?.Invoke(this, new NfsEventArgs((int)chunkCount));

                    if (size == 0)
                    {
                        totalLenght = currentPosition;
                        return;
                    }

                    Array.Copy(chunkBuffer, 0, buffer, currentPosition, size);
                    currentPosition += (uint)size;
                } while (currentPosition != totalLenght);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkBuffer);
            }
        }

        /// <summary>
        /// Copy a local file to a remote directory.
        /// </summary>
        /// <param name="destinationFileFullName">The destination file full name.</param>
        /// <param name="sourceFileFullName">The local full file path.</param>
        public void Write(string destinationFileFullName, string sourceFileFullName)
        {
            if (System.IO.File.Exists(sourceFileFullName))
            {
                System.IO.FileStream wfs = new System.IO.FileStream(sourceFileFullName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                Write(destinationFileFullName, wfs);
                wfs.Close();
            }
        }

        /// <summary>
        /// Copy a local file to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The destination full file name.</param>
        /// <param name="inputStream">The input file stream.</param>
        public void Write(string destinationFileFullName, System.IO.Stream inputStream)
        {
            Write(destinationFileFullName, 0, inputStream);
        }

        /// <summary>
        /// Copy a local file stream to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The destination full file name.</param>
        /// <param name="inputOffset">The input offset in bytes.</param>
        /// <param name="inputStream">The input stream.</param>
        public void Write(string destinationFileFullName, long inputOffset, System.IO.Stream inputStream)
        {
            Write(destinationFileFullName, inputOffset, inputStream, CancellationToken.None);
        }

        /// <summary>
        /// Copy a local file stream to a remote file with cancellation support.
        /// </summary>
        /// <param name="destinationFileFullName">The destination full file name.</param>
        /// <param name="inputOffset">The input offset in bytes.</param>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        public void Write(string destinationFileFullName, long inputOffset, System.IO.Stream inputStream, CancellationToken cancellationToken)
        {
            if (inputStream != null)
            {
                destinationFileFullName = CorrectPath(destinationFileFullName);

                if (!FileExists(destinationFileFullName))
                    CreateFile(destinationFileFullName);

                cancellationToken.ThrowIfCancellationRequested();

                _Logger.LogDebug("Writing to file {FilePath} starting at offset {Offset}", destinationFileFullName, inputOffset);

                long offset = inputOffset;
                long totalWritten = 0;

                byte[] buffer = ArrayPool<byte>.Shared.Rent(BlockSize);
                try
                {
                    int readCount, writeCount;

                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        readCount = inputStream.Read(buffer, 0, BlockSize);

                        if (readCount != 0)
                        {
                            writeCount = _NfsInterface.Write(destinationFileFullName, offset, readCount, buffer);

                            DataEvent?.Invoke(this, new NfsEventArgs(writeCount));

                            offset += readCount;
                            totalWritten += writeCount;
                        }
                    } while (readCount != 0);

                    CompleteIo();

                    _Logger.LogDebug("Completed writing to file {FilePath} ({TotalBytes} bytes written)", destinationFileFullName, totalWritten);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(inputStream), "Input stream must not be null.");
            }
        }

        /// <summary>
        /// Copy a local file to a remote directory, CompleteIO proc must called end of the writing process for system stability.
        /// </summary>
        /// <param name="destinationFileFullName">The full local file path.</param>
        /// <param name="offset">The start offset in bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="buffer">The input buffer.</param>
        public void Write(string destinationFileFullName, long offset, int count, byte[] buffer)
        {
            destinationFileFullName = CorrectPath(destinationFileFullName);

            if (!FileExists(destinationFileFullName))
                CreateFile(destinationFileFullName);

            long currentPosition = 0;

            byte[] chunkBuffer = ArrayPool<byte>.Shared.Rent(BlockSize);
            try
            {
                int writeCount = 0, writeLength = BlockSize;

                do
                {
                    if (count - currentPosition < writeLength)
                    { writeLength = (int)(count - currentPosition); }

                    Array.Copy(buffer, currentPosition, chunkBuffer, 0, writeLength);
                    writeCount = _NfsInterface.Write(destinationFileFullName, offset + currentPosition, writeLength, chunkBuffer);

                    DataEvent?.Invoke(this, new NfsEventArgs(writeCount));

                    currentPosition += writeCount;
                } while (count != currentPosition);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkBuffer);
            }
        }

        /// <summary>
        /// Copy a local file to a remote directory.
        /// </summary>
        /// <param name="destinationFileFullName">The full local file path.</param>
        /// <param name="offset">The start offset in bytes.</param>
        /// <param name="count">The number of bytes.</param>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="totalLenght">Returns the total written bytes.</param>
        public void Write(string destinationFileFullName, long offset, uint count, byte[] buffer, out uint totalLenght)
        {
            destinationFileFullName = CorrectPath(destinationFileFullName);

            totalLenght = count;
            uint blockSize = (uint)this.BlockSize;
            uint currentPosition = 0;
            if (buffer == null)
                return;

            byte[] chunkBuffer = ArrayPool<byte>.Shared.Rent((int)blockSize);
            try
            {
                do
                {
                    int size = -1;
                    uint chunkCount = blockSize;
                    if (totalLenght - currentPosition < blockSize)
                        chunkCount = (uint)totalLenght - currentPosition;

                    Array.Copy(buffer, (int)currentPosition, chunkBuffer, 0, (int)chunkCount);
                    size = _NfsInterface.Write(destinationFileFullName, offset + currentPosition, (int)chunkCount, chunkBuffer);
                    DataEvent?.Invoke(this, new NfsEventArgs((int)chunkCount));
                    if (size == 0)
                    {
                        totalLenght = currentPosition;
                        return;
                    }
                    currentPosition += (uint)chunkCount;
                } while (currentPosition != totalLenght);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkBuffer);
            }
        }

        /// <summary>
        /// Move a file from/to a directory.
        /// </summary>
        /// <param name="sourceFileFullName">The exact file location for source (e.g. "directory\sub1\sub2\filename" or "." for the root).</param>
        /// <param name="targetFileFullName">Target location of moving file (e.g. "directory\sub1\sub2\filename" or "." for the root).</param>
        public void Move(string sourceFileFullName, string targetFileFullName)
        {
            if (!IsNullOrEmpty(targetFileFullName))
            {
                if (targetFileFullName.LastIndexOf('\\') + 1 == targetFileFullName.Length)
                {
                    targetFileFullName = System.IO.Path.Combine(targetFileFullName, System.IO.Path.GetFileName(sourceFileFullName));
                }
            }

            sourceFileFullName = CorrectPath(sourceFileFullName);
            targetFileFullName = CorrectPath(targetFileFullName);

            _NfsInterface.Move(
                System.IO.Path.GetDirectoryName(sourceFileFullName),
                System.IO.Path.GetFileName(sourceFileFullName),
                System.IO.Path.GetDirectoryName(targetFileFullName),
                System.IO.Path.GetFileName(targetFileFullName)
            );
        }

        /// <summary>
        /// Check if the passed path refers to a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path (e.g. "directory\sub1\sub2" or "." for the root).</param>
        /// <returns>True if is a directory.</returns>
        public bool IsDirectory(string directoryFullName)
        {
            directoryFullName = CorrectPath(directoryFullName);

            return _NfsInterface.IsDirectory(directoryFullName);
        }

        /// <summary>
        /// Completes Current Read/Write Caching and Release Resources.
        /// </summary>
        public void CompleteIo() => _NfsInterface.CompleteIO();

        /// <summary>
        /// Check if a file/directory exists.
        /// </summary>
        /// <param name="fileFullName">The item full name.</param>
        /// <returns>True if exists.</returns>
        public bool FileExists(string fileFullName)
        {
            fileFullName = CorrectPath(fileFullName);

            return GetItemAttributes(fileFullName, false) != null;
        }

        /// <summary>
        /// Get the file/directory name from a standard windows path (eg. "\\test\text.txt" --> "text.txt" or "\\" --> ".").
        /// </summary>
        /// <param name="fileFullName">The source path.</param>
        /// <returns>The file/directory name.</returns>
        public string GetFileName(string fileFullName)
        {
            fileFullName = CorrectPath(fileFullName);

            string str = System.IO.Path.GetFileName(fileFullName);
            if (IsNullOrEmpty(str))
            {
                str = ".";
            }

            return str;
        }

        /// <summary>
        /// Get the directory name from a standard windows path (eg. "\\test\test1\text.txt" --> "test\\test1" or "\\" --> ".").
        /// </summary>
        /// <param name="fullDirectoryName">The full path(e.g. "directory/sub1/sub2" or "." for the root).</param>
        /// <returns>The directory name.</returns>
        public string GetDirectoryName(string fullDirectoryName)
        {
            fullDirectoryName = CorrectPath(fullDirectoryName);

            string str = System.IO.Path.GetDirectoryName(fullDirectoryName);
            if (IsNullOrEmpty(str))
            {
                str = ".";
            }

            return str;
        }

        /// <summary>
        /// Combine a file name to a directory (eg. FileName "test.txt", Directory "test" --> "test\test.txt" or FileName "test.txt", Directory "." --> "test.txt").
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="directoryFullName">The directory name (e.g. "directory\sub1\sub2" or "." for the root).</param>
        /// <returns>The combined path.</returns>
        public string Combine(string fileName, string directoryFullName)
        {
            directoryFullName = CorrectPath(directoryFullName);

            return $"{directoryFullName}\\{fileName}";
        }

        /// <summary>
        /// Set the file size.
        /// </summary>
        /// <param name="fileFullName">The file full path.</param>
        /// <param name="size">the size in bytes.</param>
        public void SetFileSize(string fileFullName, long size)
        {
            fileFullName = CorrectPath(fileFullName);

            _NfsInterface.SetFileSize(fileFullName, size);
        }

        /// <summary>
        /// Corrects a path entry by normalizing path separators and ensuring proper format.
        /// </summary>
        /// <param name="pathEntry">The path to correct.</param>
        /// <returns>The corrected path string.</returns>
        public static string CorrectPath(string pathEntry)
        {
            if (IsNullOrEmpty(pathEntry))
                return pathEntry;

            string[] pathList = pathEntry.Split('\\');

            pathEntry = Join("\\", pathList.Where(item => !IsNullOrEmpty(item)).ToArray());

            if (pathEntry.IndexOf('.') != 0)
            {
                pathEntry = Concat(".\\", pathEntry);
            }

            return pathEntry;
        }

        #endregion Methods

        #region Async Methods

        /// <summary>
        /// Asynchronously create a connection to a NFS Server using the specified options.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="options">The connection options. If null, default options are used.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task ConnectAsync(IPAddress address, NfsConnectionOptions? options = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Connect(address, options);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously close the current connection.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Disconnect();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously get the list of the exported NFS devices.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A list of the exported NFS devices.</returns>
        public Task<List<string>> GetExportedDevicesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetExportedDevices());
        }

        /// <summary>
        /// Asynchronously mount a device.
        /// </summary>
        /// <param name="deviceName">The device name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task MountDeviceAsync(string deviceName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MountDevice(deviceName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously unmount the current device.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task UnMountDeviceAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UnMountDevice();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously get the items in a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A list of the items name.</returns>
        public Task<List<string>> GetItemListAsync(string directoryFullName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetItemList(directoryFullName));
        }

        /// <summary>
        /// Asynchronously get the items in a directory as an async enumerable for memory-efficient enumeration.
        /// </summary>
        /// <param name="directoryFullName">Directory name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>An async enumerable of item names.</returns>
        public async IAsyncEnumerable<string> GetItemsAsync(string directoryFullName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            directoryFullName = CorrectPath(directoryFullName);
            List<string> items = _NfsInterface.GetItemList(directoryFullName);

            foreach (string item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item != "." && item != "..")
                {
                    yield return item;
                }
                await Task.Yield();
            }
        }

        /// <summary>
        /// Asynchronously get an item's attributes.
        /// </summary>
        /// <param name="itemFullName">The item full path name.</param>
        /// <param name="throwExceptionIfNotFound">If true, throws exception when item is not found.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A NFSAttributes class.</returns>
        public Task<NFSAttributes?> GetItemAttributesAsync(string itemFullName, bool throwExceptionIfNotFound = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<NFSAttributes?>(GetItemAttributes(itemFullName, throwExceptionIfNotFound));
        }

        /// <summary>
        /// Asynchronously create a new directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        /// <param name="mode">Directory permissions.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task CreateDirectoryAsync(string directoryFullName, NFSPermission? mode = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateDirectory(directoryFullName, mode ?? Mode);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously delete a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        /// <param name="recursive">If true, deletes contents recursively.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task DeleteDirectoryAsync(string directoryFullName, bool recursive = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteDirectory(directoryFullName, recursive);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously delete a file.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task DeleteFileAsync(string fileFullName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteFile(fileFullName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously create a new file.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        /// <param name="mode">File permission.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task CreateFileAsync(string fileFullName, NFSPermission? mode = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateFile(fileFullName, mode ?? Mode);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously copy a file from a remote directory to a stream.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file name.</param>
        /// <param name="outputStream">The output stream to write to.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReadAsync(string sourceFileFullName, System.IO.Stream outputStream, CancellationToken cancellationToken = default)
        {
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            sourceFileFullName = CorrectPath(sourceFileFullName);

            if (!FileExists(sourceFileFullName))
                throw new System.IO.FileNotFoundException("File not found", sourceFileFullName);

            cancellationToken.ThrowIfCancellationRequested();

            NFSAttributes nfsAttributes = GetItemAttributes(sourceFileFullName, true);
            long totalRead = nfsAttributes.Size, readOffset = 0;

            _Logger.LogDebug("Async reading file {FilePath} ({Size} bytes)", sourceFileFullName, totalRead);

            byte[] chunkBuffer = ArrayPool<byte>.Shared.Rent(BlockSize);
            try
            {
                int readCount, readLength = BlockSize;

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (totalRead < readLength)
                    {
                        readLength = (int)totalRead;
                    }

                    readCount = _NfsInterface.Read(sourceFileFullName, readOffset, readLength, ref chunkBuffer);

                    DataEvent?.Invoke(this, new NfsEventArgs(readCount));

                    await outputStream.WriteAsync(chunkBuffer, 0, readCount, cancellationToken).ConfigureAwait(false);

                    totalRead -= readCount;
                    readOffset += readCount;
                }
                while (readCount != 0);

                await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);

                CompleteIo();

                _Logger.LogDebug("Completed async reading file {FilePath}", sourceFileFullName);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkBuffer);
            }
        }

        /// <summary>
        /// Asynchronously read a chunk of a remote file into a buffer.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file full path.</param>
        /// <param name="offset">Start offset.</param>
        /// <param name="buffer">Output buffer.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The number of bytes read.</returns>
        public Task<int> ReadAsync(string sourceFileFullName, long offset, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            sourceFileFullName = CorrectPath(sourceFileFullName);

            byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int readCount = _NfsInterface.Read(sourceFileFullName, offset, buffer.Length, ref tempBuffer);

                if (readCount > 0)
                {
                    tempBuffer.AsSpan(0, readCount).CopyTo(buffer.Span);
                }

                DataEvent?.Invoke(this, new NfsEventArgs(readCount));

                return Task.FromResult(readCount);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(tempBuffer);
            }
        }

        /// <summary>
        /// Asynchronously copy a local file stream to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The destination full file name.</param>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task WriteAsync(string destinationFileFullName, System.IO.Stream inputStream, CancellationToken cancellationToken = default)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));

            destinationFileFullName = CorrectPath(destinationFileFullName);

            if (!FileExists(destinationFileFullName))
                CreateFile(destinationFileFullName);

            cancellationToken.ThrowIfCancellationRequested();

            _Logger.LogDebug("Async writing to file {FilePath}", destinationFileFullName);

            long offset = 0;
            long totalWritten = 0;

            byte[] buffer = ArrayPool<byte>.Shared.Rent(BlockSize);
            try
            {
                int readCount, writeCount;

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    readCount = await inputStream.ReadAsync(buffer, 0, BlockSize, cancellationToken).ConfigureAwait(false);

                    if (readCount != 0)
                    {
                        writeCount = _NfsInterface.Write(destinationFileFullName, offset, readCount, buffer);

                        DataEvent?.Invoke(this, new NfsEventArgs(writeCount));

                        offset += readCount;
                        totalWritten += writeCount;
                    }
                } while (readCount != 0);

                CompleteIo();

                _Logger.LogDebug("Completed async writing to file {FilePath} ({TotalBytes} bytes written)", destinationFileFullName, totalWritten);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Asynchronously write a buffer to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The full remote file path.</param>
        /// <param name="offset">The start offset in bytes.</param>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The number of bytes written.</returns>
        public Task<int> WriteAsync(string destinationFileFullName, long offset, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            destinationFileFullName = CorrectPath(destinationFileFullName);

            if (!FileExists(destinationFileFullName))
                CreateFile(destinationFileFullName);

            byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.Span.CopyTo(tempBuffer);
                int writeCount = _NfsInterface.Write(destinationFileFullName, offset, buffer.Length, tempBuffer);

                DataEvent?.Invoke(this, new NfsEventArgs(writeCount));

                return Task.FromResult(writeCount);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(tempBuffer);
            }
        }

        /// <summary>
        /// Asynchronously move a file from/to a directory.
        /// </summary>
        /// <param name="sourceFileFullName">The source file location.</param>
        /// <param name="targetFileFullName">The target file location.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task MoveAsync(string sourceFileFullName, string targetFileFullName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Move(sourceFileFullName, targetFileFullName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously set the file size.
        /// </summary>
        /// <param name="fileFullName">The file full path.</param>
        /// <param name="size">The size in bytes.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task SetFileSizeAsync(string fileFullName, long size, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetFileSize(fileFullName, size);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously check if a file/directory exists.
        /// </summary>
        /// <param name="fileFullName">The item full name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>True if exists.</returns>
        public Task<bool> FileExistsAsync(string fileFullName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(FileExists(fileFullName));
        }

        /// <summary>
        /// Asynchronously check if the passed path refers to a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>True if is a directory.</returns>
        public Task<bool> IsDirectoryAsync(string directoryFullName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(IsDirectory(directoryFullName));
        }

        #endregion Async Methods

        #region IDisposable

        /// <summary>
        /// Releases all resources used by the NfsClient instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            _Logger.LogDebug("Disposing NFS client (disposing={Disposing})", disposing);

            if (disposing)
            {
                _NfsInterface?.Dispose();
            }

            _Disposed = true;

            _Logger.LogDebug("NFS client disposed");
        }

        #endregion IDisposable

        #region IAsyncDisposable

        /// <summary>
        /// Asynchronously releases all resources used by the NfsClient instance.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs async-specific cleanup operations.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
        protected virtual ValueTask DisposeAsyncCore()
        {
            _Logger.LogDebug("Async disposing NFS client");

            if (!_Disposed && _NfsInterface != null)
            {
                _NfsInterface.Dispose();
            }

            _Logger.LogDebug("NFS client async disposed");
            return default;
        }

        #endregion IAsyncDisposable
    }
}
