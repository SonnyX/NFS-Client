namespace NFSLibrary
{
    using NFSLibrary.Protocols.Commons;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    /// <summary>
    /// Interface for NFS Client operations. Provides a mockable abstraction for NFS operations.
    /// </summary>
    public interface INfsClient : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets whether the current export is mounted.
        /// </summary>
        bool IsMounted { get; }

        /// <summary>
        /// Gets whether the connection is active.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets or sets file/folder access permissions for newly created items.
        /// </summary>
        NFSPermission Mode { get; set; }

        /// <summary>
        /// Gets the current server directory.
        /// </summary>
        string CurrentDirectory { get; }

        /// <summary>
        /// Gets or sets the block size for read/write operations.
        /// </summary>
        int BlockSize { get; }

        /// <summary>
        /// Event fired when data is transferred from/to the server.
        /// </summary>
        event NfsClient.NfsDataEventHandler DataEvent;

        /// <summary>
        /// Create a connection to a NFS Server using default options.
        /// </summary>
        /// <param name="address">The server address.</param>
        void Connect(IPAddress address);

        /// <summary>
        /// Create a connection to a NFS Server using the specified options.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="options">The connection options. If null, default options are used.</param>
        void Connect(IPAddress address, NfsConnectionOptions? options);

        /// <summary>
        /// Create a connection to a NFS Server.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="userId">The unix user id.</param>
        /// <param name="groupId">The unix group id.</param>
        /// <param name="commandTimeout">The command timeout in milliseconds.</param>
        void Connect(IPAddress address, int userId, int groupId, int commandTimeout);

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
        void Connect(IPAddress address, int userId, int groupId, int commandTimeout, System.Text.Encoding characterEncoding, bool useSecurePort, bool useCache, int nfsPort = 0, int mountPort = 0);

        /// <summary>
        /// Close the current connection.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Get the list of the exported NFS devices.
        /// </summary>
        /// <returns>A list of the exported NFS devices.</returns>
        List<string> GetExportedDevices();

        /// <summary>
        /// Mount device.
        /// </summary>
        /// <param name="deviceName">The device name.</param>
        void MountDevice(string deviceName);

        /// <summary>
        /// Unmount the current device.
        /// </summary>
        void UnMountDevice();

        /// <summary>
        /// Get the items in a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory name (e.g. "directory\subdirectory" or "." for the root).</param>
        /// <returns>A list of the items name.</returns>
        List<string> GetItemList(string directoryFullName);

        /// <summary>
        /// Get the items in a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory name (e.g. "directory\subdirectory" or "." for the root).</param>
        /// <param name="excludeNavigationDots">When posted as true, return list will not contains "." and "..".</param>
        /// <returns>A list of the items name.</returns>
        List<string> GetItemList(string directoryFullName, bool excludeNavigationDots);

        /// <summary>
        /// Get an item attributes.
        /// </summary>
        /// <param name="itemFullName">The item full path name.</param>
        /// <param name="throwExceptionIfNotFound">If true, throws exception when item is not found.</param>
        /// <returns>A NFSAttributes class.</returns>
        NFSAttributes GetItemAttributes(string itemFullName, bool throwExceptionIfNotFound = true);

        /// <summary>
        /// Create a new directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        void CreateDirectory(string directoryFullName);

        /// <summary>
        /// Create a new directory with permission.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        /// <param name="mode">Directory permissions.</param>
        void CreateDirectory(string directoryFullName, NFSPermission mode);

        /// <summary>
        /// Delete a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        void DeleteDirectory(string directoryFullName);

        /// <summary>
        /// Delete a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        /// <param name="recursive">If true, deletes contents recursively.</param>
        void DeleteDirectory(string directoryFullName, bool recursive);

        /// <summary>
        /// Delete a file.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        void DeleteFile(string fileFullName);

        /// <summary>
        /// Create a new file.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        void CreateFile(string fileFullName);

        /// <summary>
        /// Create a new file with permission.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        /// <param name="mode">File permission.</param>
        void CreateFile(string fileFullName, NFSPermission mode);

        /// <summary>
        /// Copy a set of files from a remote directory to a local directory.
        /// </summary>
        /// <param name="sourceFileNames">A list of the remote files name.</param>
        /// <param name="sourceDirectoryFullName">The remote directory path.</param>
        /// <param name="destinationDirectoryFullName">The destination local directory.</param>
        void Read(IEnumerable<string> sourceFileNames, string sourceDirectoryFullName, string destinationDirectoryFullName);

        /// <summary>
        /// Copy a file from a remote directory to a local directory.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file name.</param>
        /// <param name="destinationFileFullName">The destination local file path.</param>
        void Read(string sourceFileFullName, string destinationFileFullName);

        /// <summary>
        /// Copy a file from a remote directory to a stream.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file name.</param>
        /// <param name="outputStream">The output stream to write to.</param>
        void Read(string sourceFileFullName, ref System.IO.Stream outputStream);

        /// <summary>
        /// Copy a file from a remote directory to a stream with cancellation support.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file name.</param>
        /// <param name="outputStream">The output stream to write to.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        void Read(string sourceFileFullName, ref System.IO.Stream outputStream, CancellationToken cancellationToken);

        /// <summary>
        /// Copy a remote file to a buffer.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file full path.</param>
        /// <param name="offset">Start offset.</param>
        /// <param name="totalLength">Total length to read.</param>
        /// <param name="buffer">Output buffer.</param>
        /// <returns>The number of copied bytes.</returns>
        long Read(string sourceFileFullName, long offset, long totalLength, ref byte[] buffer);

        /// <summary>
        /// Copy a remote file to a buffer.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file full path.</param>
        /// <param name="offset">Start offset.</param>
        /// <param name="totalLength">Number of bytes (updated with actual read count).</param>
        /// <param name="buffer">Output buffer.</param>
        void Read(string sourceFileFullName, long offset, ref long totalLength, ref byte[] buffer);

        /// <summary>
        /// Copy a local file to a remote directory.
        /// </summary>
        /// <param name="destinationFileFullName">The destination file full name.</param>
        /// <param name="sourceFileFullName">The local full file path.</param>
        void Write(string destinationFileFullName, string sourceFileFullName);

        /// <summary>
        /// Copy a local file to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The destination full file name.</param>
        /// <param name="inputStream">The input file stream.</param>
        void Write(string destinationFileFullName, System.IO.Stream inputStream);

        /// <summary>
        /// Copy a local file stream to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The destination full file name.</param>
        /// <param name="inputOffset">The input offset in bytes.</param>
        /// <param name="inputStream">The input stream.</param>
        void Write(string destinationFileFullName, long inputOffset, System.IO.Stream inputStream);

        /// <summary>
        /// Copy a local file stream to a remote file with cancellation support.
        /// </summary>
        /// <param name="destinationFileFullName">The destination full file name.</param>
        /// <param name="inputOffset">The input offset in bytes.</param>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        void Write(string destinationFileFullName, long inputOffset, System.IO.Stream inputStream, CancellationToken cancellationToken);

        /// <summary>
        /// Copy a buffer to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The full remote file path.</param>
        /// <param name="offset">The start offset in bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="buffer">The input buffer.</param>
        void Write(string destinationFileFullName, long offset, int count, byte[] buffer);

        /// <summary>
        /// Copy a buffer to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The full remote file path.</param>
        /// <param name="offset">The start offset in bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="totalLength">Returns the total written bytes.</param>
        void Write(string destinationFileFullName, long offset, uint count, byte[] buffer, out uint totalLength);

        /// <summary>
        /// Move a file from/to a directory.
        /// </summary>
        /// <param name="sourceFileFullName">The source file location.</param>
        /// <param name="targetFileFullName">The target file location.</param>
        void Move(string sourceFileFullName, string targetFileFullName);

        /// <summary>
        /// Check if the passed path refers to a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path.</param>
        /// <returns>True if is a directory.</returns>
        bool IsDirectory(string directoryFullName);

        /// <summary>
        /// Completes current read/write caching and release resources.
        /// </summary>
        void CompleteIo();

        /// <summary>
        /// Check if a file/directory exists.
        /// </summary>
        /// <param name="fileFullName">The item full name.</param>
        /// <returns>True if exists.</returns>
        bool FileExists(string fileFullName);

        /// <summary>
        /// Get the file/directory name from a path.
        /// </summary>
        /// <param name="fileFullName">The source path.</param>
        /// <returns>The file/directory name.</returns>
        string GetFileName(string fileFullName);

        /// <summary>
        /// Get the directory name from a path.
        /// </summary>
        /// <param name="fullDirectoryName">The full path.</param>
        /// <returns>The directory name.</returns>
        string GetDirectoryName(string fullDirectoryName);

        /// <summary>
        /// Combine a file name to a directory.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="directoryFullName">The directory name.</param>
        /// <returns>The combined path.</returns>
        string Combine(string fileName, string directoryFullName);

        /// <summary>
        /// Set the file size.
        /// </summary>
        /// <param name="fileFullName">The file full path.</param>
        /// <param name="size">The size in bytes.</param>
        void SetFileSize(string fileFullName, long size);

        #region Async Methods

        /// <summary>
        /// Asynchronously create a connection to a NFS Server using the specified options.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="options">The connection options. If null, default options are used.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task ConnectAsync(IPAddress address, NfsConnectionOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously close the current connection.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously get the list of the exported NFS devices.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A list of the exported NFS devices.</returns>
        Task<List<string>> GetExportedDevicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously mount a device.
        /// </summary>
        /// <param name="deviceName">The device name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task MountDeviceAsync(string deviceName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously unmount the current device.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task UnMountDeviceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously get the items in a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory name (e.g. "directory\subdirectory" or "." for the root).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A list of the items name.</returns>
        Task<List<string>> GetItemListAsync(string directoryFullName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously get the items in a directory as an async enumerable.
        /// </summary>
        /// <param name="directoryFullName">Directory name (e.g. "directory\subdirectory" or "." for the root).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>An async enumerable of item names.</returns>
        IAsyncEnumerable<string> GetItemsAsync(string directoryFullName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously get an item's attributes.
        /// </summary>
        /// <param name="itemFullName">The item full path name.</param>
        /// <param name="throwExceptionIfNotFound">If true, throws exception when item is not found.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A NFSAttributes class.</returns>
        Task<NFSAttributes?> GetItemAttributesAsync(string itemFullName, bool throwExceptionIfNotFound = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously create a new directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        /// <param name="mode">Directory permissions.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task CreateDirectoryAsync(string directoryFullName, NFSPermission? mode = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete a directory.
        /// </summary>
        /// <param name="directoryFullName">Directory full name.</param>
        /// <param name="recursive">If true, deletes contents recursively.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task DeleteDirectoryAsync(string directoryFullName, bool recursive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete a file.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task DeleteFileAsync(string fileFullName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously create a new file.
        /// </summary>
        /// <param name="fileFullName">File full name.</param>
        /// <param name="mode">File permission.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task CreateFileAsync(string fileFullName, NFSPermission? mode = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously copy a file from a remote directory to a stream.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file name.</param>
        /// <param name="outputStream">The output stream to write to.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task ReadAsync(string sourceFileFullName, System.IO.Stream outputStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously read a chunk of a remote file into a buffer.
        /// </summary>
        /// <param name="sourceFileFullName">The remote file full path.</param>
        /// <param name="offset">Start offset.</param>
        /// <param name="buffer">Output buffer.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The number of bytes read.</returns>
        Task<int> ReadAsync(string sourceFileFullName, long offset, Memory<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously copy a local file stream to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The destination full file name.</param>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task WriteAsync(string destinationFileFullName, System.IO.Stream inputStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously write a buffer to a remote file.
        /// </summary>
        /// <param name="destinationFileFullName">The full remote file path.</param>
        /// <param name="offset">The start offset in bytes.</param>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The number of bytes written.</returns>
        Task<int> WriteAsync(string destinationFileFullName, long offset, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously move a file from/to a directory.
        /// </summary>
        /// <param name="sourceFileFullName">The source file location.</param>
        /// <param name="targetFileFullName">The target file location.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task MoveAsync(string sourceFileFullName, string targetFileFullName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously set the file size.
        /// </summary>
        /// <param name="fileFullName">The file full path.</param>
        /// <param name="size">The size in bytes.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SetFileSizeAsync(string fileFullName, long size, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously check if a file/directory exists.
        /// </summary>
        /// <param name="fileFullName">The item full name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>True if exists.</returns>
        Task<bool> FileExistsAsync(string fileFullName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously check if the passed path refers to a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>True if is a directory.</returns>
        Task<bool> IsDirectoryAsync(string directoryFullName, CancellationToken cancellationToken = default);

        #endregion
    }
}
