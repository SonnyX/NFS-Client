namespace NFSLibrary.Protocols.V2
{
    using NFSLibrary.Protocols.Commons;
    using NFSLibrary.Protocols.Commons.Exceptions;
    using NFSLibrary.Protocols.Commons.Exceptions.Mount;
    using NFSLibrary.Protocols.V2.RPC;
    using NFSLibrary.Protocols.V2.RPC.Mount;
    using NFSLibrary.Rpc;
    using System;
    using System.Collections.Generic;
    using System.Net;
    /// <summary>
    /// NFS version 2 protocol implementation.
    /// </summary>
    public class NFSv2 : INFS
    {
        #region Fields

        private NFSHandle? _RootDirectoryHandleObject = null;
        private NFSHandle? _CurrentItemHandleObject = null;

        private NFSv2ProtocolClient? _ProtocolV2 = null;
        private NFSv2MountProtocolClient? _MountProtocolV2 = null;

        private string _MountedDevice = string.Empty;
        private string _CurrentItem = string.Empty;

        private int _GroupId = -1;
        private int _UserId = -1;

        private bool _Disposed = false;

        #endregion Fields

        #region Constants

        /*const int MODE_FMT = 0170000;
        const int MODE_DIR = 0040000;
        const int MODE_CHR = 0020000;
        const int MODE_BLK = 0060000;
        const int MODE_REG = 0100000;
        const int MODE_LNK = 0120000;
        const int MODE_SOCK = 0140000;
        const int MODE_FIFO = 0010000;*/

        #endregion Constants

        #region Methods

        /// <summary>
        /// Connects to an NFS version 2 server.
        /// </summary>
        /// <param name="address">The IP address of the NFS server.</param>
        /// <param name="userId">The user ID for authentication.</param>
        /// <param name="groupId">The group ID for authentication.</param>
        /// <param name="clientTimeout">The timeout in milliseconds for RPC calls.</param>
        /// <param name="characterEncoding">The character encoding to use for string operations.</param>
        /// <param name="useSecurePort">Indicates whether to use a secure (privileged) port.</param>
        /// <param name="useFhCache">Indicates whether to use file handle caching (not used in NFSv2).</param>
        /// <param name="nfsPort">The NFS server port. Use 0 to discover via portmapper.</param>
        /// <param name="mountPort">The mount protocol port. Use 0 to discover via portmapper.</param>
        public void Connect(IPAddress address, int userId, int groupId, int clientTimeout, System.Text.Encoding characterEncoding, bool useSecurePort, bool useFhCache, int nfsPort = 0, int mountPort = 0)
        {
            if (clientTimeout == 0)
            { clientTimeout = 60000; }

            if (characterEncoding == null)
            { characterEncoding = System.Text.Encoding.ASCII; }

            _RootDirectoryHandleObject = null;
            _CurrentItemHandleObject = null;

            _MountedDevice = String.Empty;
            _CurrentItem = String.Empty;

            _GroupId = groupId;
            _UserId = userId;

            // Use specified mount port if provided, otherwise let portmapper discover it
            if (mountPort > 0)
            {
                _MountProtocolV2 = new NFSv2MountProtocolClient(address, mountPort, OncRpcProtocols.ONCRPC_UDP);
            }
            else
            {
                _MountProtocolV2 = new NFSv2MountProtocolClient(address, OncRpcProtocols.ONCRPC_UDP, useSecurePort);
            }

            // Use specified NFS port if provided, otherwise let portmapper discover it
            if (nfsPort > 0)
            {
                _ProtocolV2 = new NFSv2ProtocolClient(address, nfsPort, OncRpcProtocols.ONCRPC_UDP);
            }
            else
            {
                _ProtocolV2 = new NFSv2ProtocolClient(address, OncRpcProtocols.ONCRPC_UDP, useSecurePort);
            }

            OncRpcClientAuthUnix authUnix = new OncRpcClientAuthUnix(System.Environment.MachineName, userId, groupId);

            _MountProtocolV2.GetClient().SetAuth(authUnix);
            _MountProtocolV2.GetClient().SetTimeout(clientTimeout);
            _MountProtocolV2.GetClient().SetCharacterEncoding(characterEncoding.WebName);

            _ProtocolV2.GetClient().SetAuth(authUnix);
            _ProtocolV2.GetClient().SetTimeout(clientTimeout);
            _ProtocolV2.GetClient().SetCharacterEncoding(characterEncoding.WebName);
        }

        /// <summary>
        /// Disconnects from the NFS server and cleans up resources.
        /// </summary>
        public void Disconnect()
        {
            _RootDirectoryHandleObject = null;
            _CurrentItemHandleObject = null;

            _MountedDevice = String.Empty;
            _CurrentItem = String.Empty;

            if (_MountProtocolV2 != null)
            { _MountProtocolV2.Close(); }

            if (_ProtocolV2 != null)
            { _ProtocolV2.Close(); }
        }

        /// <summary>
        /// Gets the optimal block size for read and write operations.
        /// </summary>
        /// <returns>The block size in bytes.</returns>
        public int GetBlockSize()
        {
            return 8064;
        }

        /// <summary>
        /// Gets the list of exported NFS devices (mount points) available on the server.
        /// </summary>
        /// <returns>A list of exported device paths.</returns>
        public List<String> GetExportedDevices()
        {
            if (_MountProtocolV2 == null)
            { throw new NFSMountConnectionException("NFS Device not connected!"); }

            List<string> nfsDevices = new List<string>();

            Exports exp = _MountProtocolV2.MOUNTPROC_EXPORT();

            for (; ; )
            {
                nfsDevices.Add(exp.Value.MountPath.Value);
                exp = exp.Value.Next;

                if (exp.Value == null) break;
            }

            return nfsDevices;
        }

        /// <summary>
        /// Mounts an NFS device (export) from the server.
        /// </summary>
        /// <param name="deviceName">The name of the device to mount.</param>
        public void MountDevice(String deviceName)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            MountStatus mnt =
                _MountProtocolV2.MOUNTPROC_MNT(new Name(deviceName));

            if (mnt.Status == 0)
            {
                _MountedDevice = deviceName;
                _RootDirectoryHandleObject = mnt.Handle;
            }
            else
            {
                MountExceptionHelpers.ThrowException(mnt.Status);
            }
        }

        /// <summary>
        /// Unmounts the currently mounted NFS device.
        /// </summary>
        public void UnMountDevice()
        {
            if (_MountedDevice != null)
            {
                _MountProtocolV2.MOUNTPROC_UMNT(new Name(_MountedDevice));

                _RootDirectoryHandleObject = null;
                _CurrentItemHandleObject = null;

                _MountedDevice = String.Empty;
                _CurrentItem = String.Empty;
            }
        }

        /// <summary>
        /// Gets the list of items (files and directories) in a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory.</param>
        /// <returns>A list of item names in the directory.</returns>
        public List<String> GetItemList(String directoryFullName)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            List<string> ItemsList = new List<string>();

            NFSAttributes itemAttributes =
                GetItemAttributes(directoryFullName);

            if (itemAttributes != null)
            {
                ItemArguments dpRdArgs = new ItemArguments();

                dpRdArgs.Cookie = new NFSCookie(0);
                dpRdArgs.Count = 4096;
                dpRdArgs.HandleObject = new NFSHandle(itemAttributes.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);

                ItemStatus pReadDirRes;

                do
                {
                    pReadDirRes = _ProtocolV2.NFSPROC_READDIR(dpRdArgs);

                    if (pReadDirRes != null &&
                        pReadDirRes.Status == NFSStats.NFS_OK)
                    {
                        Entry pEntry =
                            pReadDirRes.OK.Entries;

                        while (pEntry != null)
                        {
                            ItemsList.Add(pEntry.Name.Value);
                            dpRdArgs.Cookie = pEntry.Cookie;
                            pEntry = pEntry.NextEntry;
                        }
                    }
                    else
                    {
                        if (pReadDirRes == null)
                        {
                            throw new NFSGeneralException("NFSPROC_READDIR: failure");
                        }

                        ExceptionHelpers.ThrowException(pReadDirRes.Status);
                    }
                } while (pReadDirRes != null && !pReadDirRes.OK.EOF);
            }
            else
            {
                ExceptionHelpers.ThrowException(NFSStats.NFSERR_NOENT);
            }

            return ItemsList;
        }

        /// <summary>
        /// Gets the attributes of a file or directory.
        /// </summary>
        /// <param name="itemFullName">The full path of the item.</param>
        /// <param name="throwExceptionIfNotFound">Indicates whether to throw an exception if the item is not found.</param>
        /// <returns>The item attributes, or null if not found and throwExceptionIfNotFound is false.</returns>
        public NFSAttributes GetItemAttributes(String itemFullName, bool throwExceptionIfNotFound = true)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            NFSAttributes attributes = null;

            if (String.IsNullOrEmpty(itemFullName))
                itemFullName = ".";

            NFSHandle currentItem = _RootDirectoryHandleObject;
            String[] PathTree = itemFullName.Split(@"\".ToCharArray());

            for (int pC = 0; pC < PathTree.Length; pC++)
            {
                ItemOperationArguments dpDrArgs = new ItemOperationArguments();
                dpDrArgs.Directory = currentItem;
                dpDrArgs.Name = new Name(PathTree[pC]);

                ItemOperationStatus pDirOpRes =
                    _ProtocolV2.NFSPROC_LOOKUP(dpDrArgs);

                if (pDirOpRes != null &&
                    pDirOpRes.Status == NFSStats.NFS_OK)
                {
                    currentItem = pDirOpRes.OK.HandleObject;

                    if (PathTree.Length - 1 == pC)
                    {
                        attributes = new NFSAttributes(
                                        pDirOpRes.OK.Attributes.CreateTime.Seconds,
                                        pDirOpRes.OK.Attributes.LastAccessedTime.Seconds,
                                        pDirOpRes.OK.Attributes.ModifiedTime.Seconds,
                                        pDirOpRes.OK.Attributes.Type,
                                        pDirOpRes.OK.Attributes.Mode,
                                        pDirOpRes.OK.Attributes.Size,
                                        pDirOpRes.OK.HandleObject.Value);
                    }
                }
                else
                {
                    if (pDirOpRes == null || pDirOpRes.Status == NFSStats.NFSERR_NOENT)
                    {
                        attributes = null;
                        break;
                    }

                    if (throwExceptionIfNotFound)
                        ExceptionHelpers.ThrowException(pDirOpRes.Status);
                }
            }

            return attributes;
        }

        /// <summary>
        /// Creates a new directory on the NFS server.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory to create.</param>
        /// <param name="mode">The permissions to set on the directory.</param>
        public void CreateDirectory(String directoryFullName, NFSPermission mode)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            if (mode == null)
            {
                mode = new NFSPermission(7, 7, 7);
            }

            string ParentDirectory = System.IO.Path.GetDirectoryName(directoryFullName);
            string DirectoryName = System.IO.Path.GetFileName(directoryFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            CreateArguments dpArgCreate = new CreateArguments();
            dpArgCreate.Attributes = new CreateAttributes();
            dpArgCreate.Attributes.LastAccessedTime = new NFSTimeValue();
            dpArgCreate.Attributes.ModifiedTime = new NFSTimeValue();
            dpArgCreate.Attributes.Mode = mode;
            dpArgCreate.Attributes.UserID = this._UserId;
            dpArgCreate.Attributes.GroupID = this._GroupId;
            dpArgCreate.Where = new ItemOperationArguments();
            dpArgCreate.Where.Directory = new NFSHandle(ParentItemAttributes.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);
            dpArgCreate.Where.Name = new Name(DirectoryName);

            ItemOperationStatus pDirOpRes =
                _ProtocolV2.NFSPROC_MKDIR(dpArgCreate);

            if (pDirOpRes == null ||
                pDirOpRes.Status != NFSStats.NFS_OK)
            {
                if (pDirOpRes == null)
                {
                    throw new NFSGeneralException("NFSPROC_MKDIR: failure");
                }

                ExceptionHelpers.ThrowException(pDirOpRes.Status);
            }
        }

        /// <summary>
        /// Deletes a directory from the NFS server.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory to delete.</param>
        public void DeleteDirectory(string directoryFullName)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            string ParentDirectory = System.IO.Path.GetDirectoryName(directoryFullName);
            string DirectoryName = System.IO.Path.GetFileName(directoryFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            ItemOperationArguments dpArgDelete = new ItemOperationArguments();
            dpArgDelete.Directory = new NFSHandle(ParentItemAttributes.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);
            dpArgDelete.Name = new Name(DirectoryName);

            NFSStats Status = (NFSStats)_ProtocolV2.NFSPROC_RMDIR(dpArgDelete);

            if (Status != NFSStats.NFS_OK)
            {
                ExceptionHelpers.ThrowException(Status);
            }
        }

        /// <summary>
        /// Deletes a file from the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to delete.</param>
        public void DeleteFile(string fileFullName)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            string ParentDirectory = System.IO.Path.GetDirectoryName(fileFullName);
            string FileName = System.IO.Path.GetFileName(fileFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            ItemOperationArguments dpArgDelete = new ItemOperationArguments();
            dpArgDelete.Directory = new NFSHandle(ParentItemAttributes.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);
            dpArgDelete.Name = new Name(FileName);

            NFSStats Status = (NFSStats)_ProtocolV2.NFSPROC_REMOVE(dpArgDelete);

            if (Status != NFSStats.NFS_OK)
            {
                ExceptionHelpers.ThrowException(Status);
            }
        }

        /// <summary>
        /// Creates a new file on the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to create.</param>
        /// <param name="mode">The permissions to set on the file.</param>
        public void CreateFile(string fileFullName, NFSPermission mode)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            if (mode == null)
            {
                mode = new NFSPermission(7, 7, 7);
            }

            string ParentDirectory = System.IO.Path.GetDirectoryName(fileFullName);
            string FileName = System.IO.Path.GetFileName(fileFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            CreateArguments dpArgCreate = new CreateArguments();
            dpArgCreate.Attributes = new CreateAttributes();
            dpArgCreate.Attributes.LastAccessedTime = new NFSTimeValue();
            dpArgCreate.Attributes.ModifiedTime = new NFSTimeValue();
            dpArgCreate.Attributes.Mode = mode;
            dpArgCreate.Attributes.UserID = this._UserId;
            dpArgCreate.Attributes.GroupID = this._GroupId;
            dpArgCreate.Attributes.Size = 0;
            dpArgCreate.Where = new ItemOperationArguments();
            dpArgCreate.Where.Directory = new NFSHandle(ParentItemAttributes.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);
            dpArgCreate.Where.Name = new Name(FileName);

            ItemOperationStatus pDirOpRes =
                _ProtocolV2.NFSPROC_CREATE(dpArgCreate);

            if (pDirOpRes == null ||
                pDirOpRes.Status != NFSStats.NFS_OK)
            {
                if (pDirOpRes == null)
                {
                    throw new NFSGeneralException("NFSPROC_CREATE: failure");
                }

                ExceptionHelpers.ThrowException(pDirOpRes.Status);
            }
        }

        /// <summary>
        /// Reads data from a file on the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to read.</param>
        /// <param name="offset">The byte offset in the file to start reading.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="buffer">The buffer to store the read data.</param>
        /// <returns>The number of bytes actually read.</returns>
        public int Read(String fileFullName, long offset, int count, ref Byte[] buffer)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            int rCount = 0;

            if (count == 0)
                return 0;

            if (_CurrentItem != fileFullName)
            {
                NFSAttributes Attributes = GetItemAttributes(fileFullName);
                _CurrentItemHandleObject = new NFSHandle(Attributes.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);
                _CurrentItem = fileFullName;
            }

            ReadArguments dpArgRead = new ReadArguments();
            dpArgRead.File = _CurrentItemHandleObject;
            dpArgRead.Offset = (int)offset;
            dpArgRead.Count = count;

            ReadStatus pReadRes =
                _ProtocolV2.NFSPROC_READ(dpArgRead);

            if (pReadRes != null)
            {
                if (pReadRes.Status != NFSStats.NFS_OK)
                { ExceptionHelpers.ThrowException(pReadRes.Status); }

                rCount = pReadRes.OK.Data.Length;

                Array.Copy(pReadRes.OK.Data, buffer, rCount);
            }
            else
            {
                throw new NFSGeneralException("NFSPROC_READ: failure");
            }

            return rCount;
        }

        /// <summary>
        /// Sets the size of a file on the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file.</param>
        /// <param name="size">The new size of the file in bytes.</param>
        public void SetFileSize(string fileFullName, long size)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            NFSAttributes Attributes = GetItemAttributes(fileFullName);

            FileArguments dpArgSAttr = new FileArguments();
            dpArgSAttr.Attributes = new CreateAttributes();
            dpArgSAttr.Attributes.LastAccessedTime = new NFSTimeValue();
            dpArgSAttr.Attributes.LastAccessedTime.Seconds = -1;
            dpArgSAttr.Attributes.LastAccessedTime.UnixSeconds = -1;
            dpArgSAttr.Attributes.ModifiedTime = new NFSTimeValue();
            dpArgSAttr.Attributes.ModifiedTime.Seconds = -1;
            dpArgSAttr.Attributes.ModifiedTime.UnixSeconds = -1;
            dpArgSAttr.Attributes.Mode = new NFSPermission(0xff, 0xff, 0xff);
            dpArgSAttr.Attributes.UserID = -1;
            dpArgSAttr.Attributes.GroupID = -1;
            dpArgSAttr.Attributes.Size = (int)size;
            dpArgSAttr.File = new NFSHandle(Attributes.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);

            FileStatus pAttrStat =
                _ProtocolV2.NFSPROC_SETATTR(dpArgSAttr);

            if (pAttrStat == null || pAttrStat.Status != NFSStats.NFS_OK)
            {
                if (pAttrStat == null)
                {
                    throw new NFSGeneralException("NFSPROC_SETATTR: failure");
                }

                ExceptionHelpers.ThrowException(pAttrStat.Status);
            }
        }

        /// <summary>
        /// Writes data to a file on the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to write.</param>
        /// <param name="offset">The byte offset in the file to start writing.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="buffer">The buffer containing the data to write.</param>
        /// <returns>The number of bytes actually written.</returns>
        public int Write(String fileFullName, long offset, int count, Byte[] buffer)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            int rCount = 0;

            if (_CurrentItem != fileFullName)
            {
                NFSAttributes Attributes = GetItemAttributes(fileFullName);
                _CurrentItemHandleObject = new NFSHandle(Attributes.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);
                _CurrentItem = fileFullName;
            }

            if (count < buffer.Length)
            {
                Array.Resize<byte>(ref buffer, count);
            }

            WriteArguments dpArgWrite = new WriteArguments();
            dpArgWrite.File = _CurrentItemHandleObject;
            dpArgWrite.Offset = (int)offset;
            dpArgWrite.Data = buffer;

            FileStatus pAttrStat =
                _ProtocolV2.NFSPROC_WRITE(dpArgWrite);

            if (pAttrStat != null)
            {
                if (pAttrStat.Status != NFSStats.NFS_OK)
                {
                    ExceptionHelpers.ThrowException(pAttrStat.Status);
                }

                rCount = count;
            }
            else
            {
                throw new NFSGeneralException("NFSPROC_WRITE: failure");
            }

            return rCount;
        }

        /// <summary>
        /// Moves or renames a file or directory on the NFS server.
        /// </summary>
        /// <param name="oldDirectoryFullName">The full path of the source directory.</param>
        /// <param name="oldFileName">The name of the source file or directory.</param>
        /// <param name="newDirectoryFullName">The full path of the destination directory.</param>
        /// <param name="newFileName">The new name of the file or directory.</param>
        public void Move(string oldDirectoryFullName, string oldFileName, string newDirectoryFullName, string newFileName)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            NFSAttributes OldDirectory = GetItemAttributes(oldDirectoryFullName);
            NFSAttributes NewDirectory = GetItemAttributes(newDirectoryFullName);

            RenameArguments dpArgRename = new RenameArguments();
            dpArgRename.From = new ItemOperationArguments();
            dpArgRename.From.Directory = new NFSHandle(OldDirectory.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);
            dpArgRename.From.Name = new Name(oldFileName);
            dpArgRename.To = new ItemOperationArguments();
            dpArgRename.To.Directory = new NFSHandle(NewDirectory.Handle, V2.RPC.NFSv2Protocol.NFS_VERSION);
            dpArgRename.To.Name = new Name(newFileName);

            NFSStats Status =
                (NFSStats)_ProtocolV2.NFSPROC_RENAME(dpArgRename);

            if (Status != NFSStats.NFS_OK)
            {
                ExceptionHelpers.ThrowException(Status);
            }
        }

        /// <summary>
        /// Determines whether the specified path is a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path to check.</param>
        /// <returns>True if the path is a directory; otherwise, false.</returns>
        public bool IsDirectory(string directoryFullName)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            NFSAttributes Attributes = GetItemAttributes(directoryFullName);

            return (Attributes != null && Attributes.NFSType == NFSItemTypes.NFDIR);
        }

        /// <summary>
        /// Completes the current I/O operation and clears file handle cache.
        /// </summary>
        public void CompleteIO()
        {
            _CurrentItemHandleObject = null;
            _CurrentItem = string.Empty;
        }

        /// <summary>
        /// Creates a symbolic link on the NFS server.
        /// </summary>
        /// <param name="linkPath">The full path where the symbolic link will be created.</param>
        /// <param name="targetPath">The path that the symbolic link points to.</param>
        /// <param name="mode">The permissions to set on the symbolic link.</param>
        public void CreateSymbolicLink(string linkPath, string targetPath, NFSPermission mode)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            string linkDirectory = System.IO.Path.GetDirectoryName(linkPath) ?? ".";
            string linkName = System.IO.Path.GetFileName(linkPath);

            NFSAttributes dirAttributes = GetItemAttributes(linkDirectory);
            if (dirAttributes == null)
            {
                throw new NFSGeneralException($"Directory not found: {linkDirectory}");
            }

            SymlinkArguments symlinkArgs = new SymlinkArguments();

            // Use reflection to set private fields (RPC generated types have read-only properties)
            ItemOperationArguments fromArgs = new ItemOperationArguments();
            fromArgs.Directory = new NFSHandle(dirAttributes.Handle, NFSv2Protocol.NFS_VERSION);
            fromArgs.Name = new Name(linkName);
            typeof(SymlinkArguments).GetField("_From", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(symlinkArgs, fromArgs);

            typeof(SymlinkArguments).GetField("_To", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(symlinkArgs, new Name(targetPath));

            CreateAttributes createAttrs = new CreateAttributes();
            createAttrs.Mode = mode;
            createAttrs.UserID = _UserId;
            createAttrs.GroupID = _GroupId;
            createAttrs.LastAccessedTime = new NFSTimeValue();
            createAttrs.ModifiedTime = new NFSTimeValue();
            typeof(SymlinkArguments).GetField("_Attributes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(symlinkArgs, createAttrs);

            NFSStats status = (NFSStats)_ProtocolV2.NFSPROC_SYMLINK(symlinkArgs);

            if (status != NFSStats.NFS_OK)
            {
                ExceptionHelpers.ThrowException(status);
            }
        }

        /// <summary>
        /// Creates a hard link on the NFS server.
        /// </summary>
        /// <param name="linkPath">The full path where the hard link will be created.</param>
        /// <param name="targetPath">The path to the target file.</param>
        public void CreateHardLink(string linkPath, string targetPath)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            string linkDirectory = System.IO.Path.GetDirectoryName(linkPath) ?? ".";
            string linkName = System.IO.Path.GetFileName(linkPath);

            NFSAttributes targetAttributes = GetItemAttributes(targetPath);
            if (targetAttributes == null)
            {
                throw new NFSGeneralException($"Target file not found: {targetPath}");
            }

            NFSAttributes dirAttributes = GetItemAttributes(linkDirectory);
            if (dirAttributes == null)
            {
                throw new NFSGeneralException($"Directory not found: {linkDirectory}");
            }

            LinkArguments linkArgs = new LinkArguments();

            // Use reflection to set private fields (RPC generated types have read-only properties)
            typeof(LinkArguments).GetField("_From", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(linkArgs, new NFSHandle(targetAttributes.Handle, NFSv2Protocol.NFS_VERSION));

            ItemOperationArguments toArgs = new ItemOperationArguments();
            toArgs.Directory = new NFSHandle(dirAttributes.Handle, NFSv2Protocol.NFS_VERSION);
            toArgs.Name = new Name(linkName);
            typeof(LinkArguments).GetField("_To", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(linkArgs, toArgs);

            NFSStats status = (NFSStats)_ProtocolV2.NFSPROC_LINK(linkArgs);

            if (status != NFSStats.NFS_OK)
            {
                ExceptionHelpers.ThrowException(status);
            }
        }

        /// <summary>
        /// Reads the target path of a symbolic link.
        /// </summary>
        /// <param name="linkPath">The full path of the symbolic link.</param>
        /// <returns>The path that the symbolic link points to.</returns>
        public string ReadSymbolicLink(string linkPath)
        {
            if (_ProtocolV2 == null)
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }

            if (_MountProtocolV2 == null)
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }

            NFSAttributes linkAttributes = GetItemAttributes(linkPath);
            if (linkAttributes == null)
            {
                throw new NFSGeneralException($"Symbolic link not found: {linkPath}");
            }

            NFSHandle handle = new NFSHandle(linkAttributes.Handle, NFSv2Protocol.NFS_VERSION);
            LinkStatus result = _ProtocolV2.NFSPROC_READLINK(handle);

            if (result == null || result.Status != NFSStats.NFS_OK)
            {
                if (result == null)
                {
                    throw new NFSGeneralException("NFSPROC_READLINK: failure");
                }
                ExceptionHelpers.ThrowException(result.Status);
            }

            return result.LinkName.Value;
        }

        #endregion Methods

        #region IDisposable

        /// <summary>
        /// Releases all resources used by the NFSv2 instance.
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

            if (disposing)
            {
                Disconnect();
            }

            _Disposed = true;
        }

        #endregion IDisposable
    }
}