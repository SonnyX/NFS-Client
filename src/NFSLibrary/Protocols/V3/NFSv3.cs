namespace NFSLibrary.Protocols.V3
{
    using NFSLibrary.Protocols.Commons;
    using NFSLibrary.Protocols.Commons.Exceptions;
    using NFSLibrary.Protocols.Commons.Exceptions.Mount;
    using NFSLibrary.Protocols.V3.RPC;
    using NFSLibrary.Protocols.V3.RPC.Mount;
    using NFSLibrary.Rpc;
    using System;
    using System.Collections.Generic;
    using System.Net;
    /// <summary>
    /// NFS version 3 protocol implementation.
    /// </summary>
    public class NFSv3 : NFSProtocolBase
    {
        #region Fields

        // Protocol-specific clients
        private NFSv3ProtocolClient? _ProtocolV3 = null;
        private NFSv3MountProtocolClient? _MountProtocolV3 = null;

        #endregion Fields

        #region Connection Methods

        /// <inheritdoc/>
        protected override bool IsProtocolClientConnected() => _ProtocolV3 != null;

        /// <inheritdoc/>
        protected override bool IsMountProtocolClientConnected() => _MountProtocolV3 != null;

        /// <inheritdoc/>
        protected override void DisposeProtocolClients()
        {
            Disconnect();
        }

        /// <inheritdoc/>
        public override void Connect(IPAddress address, int userId, int groupId, int clientTimeout, System.Text.Encoding characterEncoding, bool useSecurePort, bool useFhCache, int nfsPort = 0, int mountPort = 0)
        {
            if (clientTimeout == 0)
            { clientTimeout = 60000; }

            if (characterEncoding == null)
            { characterEncoding = System.Text.Encoding.ASCII; }

            ResetConnectionState();

            _GroupID = groupId;
            _UserID = userId;

            OncRpcClientAuthUnix authUnix = new OncRpcClientAuthUnix(address.ToString(), userId, groupId);

            // Use specified mount port if provided, otherwise let portmapper discover it
            if (mountPort > 0)
            {
                _MountProtocolV3 = new NFSv3MountProtocolClient(address, mountPort, OncRpcProtocols.ONCRPC_UDP);
            }
            else
            {
                _MountProtocolV3 = new NFSv3MountProtocolClient(address, OncRpcProtocols.ONCRPC_UDP, useSecurePort);
            }

            _MountProtocolV3.GetClient().SetAuth(authUnix);
            _MountProtocolV3.GetClient().SetTimeout(clientTimeout);
            _MountProtocolV3.GetClient().SetCharacterEncoding(characterEncoding.WebName);

            // Use specified NFS port if provided, otherwise let portmapper discover it
            if (nfsPort > 0)
            {
                _ProtocolV3 = new NFSv3ProtocolClient(address, nfsPort, OncRpcProtocols.ONCRPC_TCP);
            }
            else
            {
                _ProtocolV3 = new NFSv3ProtocolClient(address, OncRpcProtocols.ONCRPC_TCP, useSecurePort);
            }

            _ProtocolV3.GetClient().SetAuth(authUnix);
            _ProtocolV3.GetClient().SetTimeout(clientTimeout);
            _ProtocolV3.GetClient().SetCharacterEncoding(characterEncoding.WebName);
        }

        #endregion Connection Methods

        #region Public Methods

        /// <inheritdoc/>
        public override void Disconnect()
        {
            ResetConnectionState();

            if (_MountProtocolV3 != null)
                _MountProtocolV3.Close();

            if (_ProtocolV3 != null)
                _ProtocolV3.Close();
        }

        /// <inheritdoc/>
        public override int GetBlockSize()
        {
            //call fsinfo
            FSInfoArguments argsfs = new FSInfoArguments();
            argsfs.FSRoot = _RootDirectoryHandleObject;
            ResultObject<FSInfoAccessOK, FSInfoAccessFAIL> fsinfo =
             _ProtocolV3.NFSPROC3_FSINFO(argsfs);

            if (fsinfo.Status == NFSStats.NFS_OK)
            {
                int maxTRrate = fsinfo.OK.PreferredReadRequestSize;
                int maxRXrate = fsinfo.OK.PreferredWriteRequestSize;

                if (maxTRrate > 8000 && maxRXrate > 8000)
                {
                    if (maxTRrate > 65336 && maxRXrate > 65336)
                        return 65336;
                    else if (maxTRrate == maxRXrate)
                        return maxTRrate - 200;
                    else if (maxTRrate < maxRXrate)
                        return maxTRrate - 200;
                    else
                        return maxRXrate - 200;
                }
            }
            return 8000;
        }

        /// <inheritdoc/>
        public override List<String> GetExportedDevices()
        {
            ValidateMountConnection();

            List<string> nfsDevices = new List<string>();

            Exports exp = _MountProtocolV3!.MOUNTPROC3_EXPORT();

            for (; ; )
            {
                nfsDevices.Add(exp.Value.MountPath.Value);
                exp = exp.Value.Next;

                if (exp.Value == null) break;
            }

            return nfsDevices;
        }

        /// <inheritdoc/>
        public override void MountDevice(String deviceName)
        {
            ValidateConnection();

            MountStatus mnt =
                _MountProtocolV3.MOUNTPROC3_MNT(new Name(deviceName));

            if (mnt.Status == NFSMountStats.MNT_OK)
            {
                _MountedDevice = deviceName;
                _RootDirectoryHandleObject = mnt.MountInfo.MountHandle;
            }
            else
            { MountExceptionHelpers.ThrowException(mnt.Status); }

            /*

            //custom
            FSStatisticsArguments argsfs2 = new FSStatisticsArguments();
            argsfs2.FSRoot = _RootDirectoryHandleObject;
            ResultObject<FSStatisticsAccessOK, FSStatisticsAccessFAIL> fstat =
                _ProtocolV3.NFSPROC3_FSSTAT(argsfs2);*/
        }

        /// <inheritdoc/>
        public override void UnMountDevice()
        {
            if (_MountedDevice != null)
            {
                _MountProtocolV3?.MOUNTPROC3_UMNT(new Name(_MountedDevice));
                ResetMountState();
            }
        }

        /// <inheritdoc/>
        public override List<String> GetItemList(String directoryFullName)
        {
            ValidateConnection();

            List<string> ItemsList = new List<string>();

            NFSAttributes itemAttributes =
                GetItemAttributes(directoryFullName);

            if (itemAttributes != null)
            {
                ReadFolderArguments dpRdArgs = new ReadFolderArguments();

                dpRdArgs.Count = 4096;
                dpRdArgs.Cookie = new NFSCookie(0);
                dpRdArgs.CookieData = new byte[NFSv3Protocol.NFS3_COOKIEVERFSIZE];
                dpRdArgs.HandleObject = new NFSHandle(itemAttributes.Handle, V3.RPC.NFSv3Protocol.NFS_V3);

                ResultObject<ReadFolderAccessResultOK, ReadFolderAccessResultFAIL> pReadDirRes;

                do
                {
                    pReadDirRes = _ProtocolV3.NFSPROC3_READDIR(dpRdArgs);

                    if (pReadDirRes != null &&
                        pReadDirRes.Status == NFSStats.NFS_OK)
                    {
                        Entry pEntry =
                            pReadDirRes.OK.Reply.Entries;

                        Array.Copy(pReadDirRes.OK.CookieData, dpRdArgs.CookieData, NFSv3Protocol.NFS3_COOKIEVERFSIZE);
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
                        { throw new NFSGeneralException("NFSPROC3_READDIR: failure"); }

                        if (pReadDirRes.Status != NFSStats.NFS_OK)
                        { ExceptionHelpers.ThrowException(pReadDirRes.Status); }
                    }
                } while (pReadDirRes != null && !pReadDirRes.OK.Reply.EOF);
            }
            else
            { ExceptionHelpers.ThrowException(NFSStats.NFSERR_NOENT); }

            return ItemsList;
        }

        /// <inheritdoc/>
        public override NFSAttributes? GetItemAttributes(string itemFullName, bool throwExceptionIfNotFound = true)
        {
            ValidateConnection();

            NFSAttributes? attributes = null;

            itemFullName = NormalizePath(itemFullName);

            NFSHandle currentItem = _RootDirectoryHandleObject;
            String[] PathTree = itemFullName.Split(@"\".ToCharArray());

            for (int pC = 0; pC < PathTree.Length; pC++)
            {
                ItemOperationArguments dpDrArgs = new ItemOperationArguments();
                dpDrArgs.Directory = currentItem;
                dpDrArgs.Name = new Name(PathTree[pC]);

                ResultObject<ItemOperationAccessResultOK, ItemOperationAccessResultFAIL> pDirOpRes =
                    _ProtocolV3.NFSPROC3_LOOKUP(dpDrArgs);

                if (pDirOpRes != null &&
                    pDirOpRes.Status == NFSStats.NFS_OK)
                {
                    currentItem = pDirOpRes.OK.ItemHandle;

                    if (PathTree.Length - 1 == pC)
                    {
                        attributes = new NFSAttributes(
                                        pDirOpRes.OK.ItemAttributes.Attributes.CreateTime.Seconds,
                                        pDirOpRes.OK.ItemAttributes.Attributes.LastAccessedTime.Seconds,
                                        pDirOpRes.OK.ItemAttributes.Attributes.ModifiedTime.Seconds,
                                        pDirOpRes.OK.ItemAttributes.Attributes.Type,
                                        pDirOpRes.OK.ItemAttributes.Attributes.Mode,
                                        pDirOpRes.OK.ItemAttributes.Attributes.Size,
                                        pDirOpRes.OK.ItemHandle.Value);
                    }
                }
                else
                {
                    if (pDirOpRes == null || pDirOpRes.Status == NFSStats.NFSERR_NOENT)
                    { attributes = null; break; }

                    if (throwExceptionIfNotFound)
                        ExceptionHelpers.ThrowException(pDirOpRes.Status);
                }
            }

            return attributes;
        }

        /// <inheritdoc/>
        public override void CreateDirectory(string directoryFullName, NFSPermission mode)
        {
            ValidateConnection();

            if (mode == null)
            { mode = new NFSPermission(7, 7, 7); }

            string ParentDirectory = System.IO.Path.GetDirectoryName(directoryFullName);
            string DirectoryName = System.IO.Path.GetFileName(directoryFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            MakeFolderArguments dpArgCreate = new MakeFolderArguments();
            dpArgCreate.Attributes = new MakeAttributes();
            dpArgCreate.Attributes.LastAccessedTime = new NFSTimeValue();
            dpArgCreate.Attributes.ModifiedTime = new NFSTimeValue();
            dpArgCreate.Attributes.Mode = mode;
            dpArgCreate.Attributes.SetMode = true;
            dpArgCreate.Attributes.UserID = this._UserID;
            dpArgCreate.Attributes.SetUserID = true;
            dpArgCreate.Attributes.GroupID = this._GroupID;
            dpArgCreate.Attributes.SetGroupID = true;
            dpArgCreate.Where = new ItemOperationArguments();
            dpArgCreate.Where.Directory = new NFSHandle(ParentItemAttributes.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
            dpArgCreate.Where.Name = new Name(DirectoryName);

            ResultObject<MakeFolderAccessOK, MakeFolderAccessFAIL> pDirOpRes =
                _ProtocolV3.NFSPROC3_MKDIR(dpArgCreate);

            if (pDirOpRes == null ||
                pDirOpRes.Status != NFSStats.NFS_OK)
            {
                if (pDirOpRes == null)
                { throw new NFSGeneralException("NFSPROC3_MKDIR: failure"); }

                ExceptionHelpers.ThrowException(pDirOpRes.Status);
            }
        }

        /// <inheritdoc/>
        public override void DeleteDirectory(string directoryFullName)
        {
            ValidateConnection();

            string ParentDirectory = GetParentDirectory(directoryFullName);
            string DirectoryName = System.IO.Path.GetFileName(directoryFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            ItemOperationArguments dpArgDelete = new ItemOperationArguments();
            dpArgDelete.Directory = new NFSHandle(ParentItemAttributes.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
            dpArgDelete.Name = new Name(DirectoryName);

            ResultObject<RemoveAccessOK, RemoveAccessFAIL> pRmDirRes =
                _ProtocolV3.NFSPROC3_RMDIR(dpArgDelete);

            if (pRmDirRes == null || pRmDirRes.Status != NFSStats.NFS_OK)
            {
                if (pRmDirRes == null)
                { throw new NFSGeneralException("NFSPROC3_RMDIR: failure"); }

                ExceptionHelpers.ThrowException(pRmDirRes.Status);
            }
        }

        /// <inheritdoc/>
        public override void DeleteFile(string fileFullName)
        {
            ValidateConnection();

            string ParentDirectory = GetParentDirectory(fileFullName);
            string FileName = System.IO.Path.GetFileName(fileFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            ItemOperationArguments dpArgDelete = new ItemOperationArguments();
            dpArgDelete.Directory = new NFSHandle(ParentItemAttributes.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
            dpArgDelete.Name = new Name(FileName);

            ResultObject<RemoveAccessOK, RemoveAccessFAIL> pRemoveRes =
                _ProtocolV3.NFSPROC3_REMOVE(dpArgDelete);

            if (pRemoveRes == null || pRemoveRes.Status != NFSStats.NFS_OK)
            {
                if (pRemoveRes == null)
                { throw new NFSGeneralException("NFSPROC3_REMOVE: failure"); }

                ExceptionHelpers.ThrowException(pRemoveRes.Status);
            }
        }

        /// <inheritdoc/>
        public override void CreateFile(string fileFullName, NFSPermission mode)
        {
            ValidateConnection();

            if (mode == null)
            { mode = new NFSPermission(7, 7, 7); }

            string ParentDirectory = GetParentDirectory(fileFullName);
            string FileName = System.IO.Path.GetFileName(fileFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            MakeFileArguments dpArgCreate = new MakeFileArguments();
            dpArgCreate.How = new MakeFileHow();
            dpArgCreate.How.Mode = MakeFileModes.UNCHECKED;
            dpArgCreate.How.Attributes = new MakeAttributes();
            dpArgCreate.How.Attributes.LastAccessedTime = new NFSTimeValue();
            dpArgCreate.How.Attributes.ModifiedTime = new NFSTimeValue();
            dpArgCreate.How.Attributes.Mode = mode;
            dpArgCreate.How.Attributes.SetMode = true;
            dpArgCreate.How.Attributes.UserID = this._UserID;
            dpArgCreate.How.Attributes.SetUserID = true;
            dpArgCreate.How.Attributes.GroupID = this._GroupID;
            dpArgCreate.How.Attributes.SetGroupID = true;
            dpArgCreate.How.Attributes.Size = 0;
            dpArgCreate.How.Attributes.SetSize = true;
            dpArgCreate.How.Verification = new byte[NFSv3Protocol.NFS3_CREATEVERFSIZE];
            dpArgCreate.Where = new ItemOperationArguments();
            dpArgCreate.Where.Directory = new NFSHandle(ParentItemAttributes.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
            dpArgCreate.Where.Name = new Name(FileName);

            ResultObject<MakeFileAccessOK, MakeFileAccessFAIL> pCreateRes =
                _ProtocolV3.NFSPROC3_CREATE(dpArgCreate);

            if (pCreateRes == null ||
                pCreateRes.Status != NFSStats.NFS_OK)
            {
                if (pCreateRes == null)
                { throw new NFSGeneralException("NFSPROC3_CREATE: failure"); }

                ExceptionHelpers.ThrowException(pCreateRes.Status);
            }
        }

        /// <inheritdoc/>
        public override int Read(String fileFullName, long offset, int count, ref Byte[] buffer)
        {
            ValidateConnection();

            int rCount = 0;

            if (count == 0)
                return 0;

            if (_CurrentItem != fileFullName)
            {
                NFSAttributes Attributes = GetItemAttributes(fileFullName);
                _CurrentItemHandleObject = new NFSHandle(Attributes.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
                _CurrentItem = fileFullName;
            }

            ReadArguments dpArgRead = new ReadArguments();
            dpArgRead.File = _CurrentItemHandleObject;
            dpArgRead.Offset = offset;
            dpArgRead.Count = count;

            ResultObject<ReadAccessOK, ReadAccessFAIL> pReadRes =
                _ProtocolV3.NFSPROC3_READ(dpArgRead);

            if (pReadRes != null)
            {
                if (pReadRes.Status != NFSStats.NFS_OK)
                { ExceptionHelpers.ThrowException(pReadRes.Status); }

                rCount = pReadRes.OK.Data.Length;

                Array.Copy(pReadRes.OK.Data, buffer, rCount);
            }
            else
            { throw new NFSGeneralException("NFSPROC3_READ: failure"); }

            return rCount;
        }

        /// <inheritdoc/>
        public override void SetFileSize(string fileFullName, long size)
        {
            ValidateConnection();

            NFSAttributes? Attributes = GetItemAttributes(fileFullName);
            if (Attributes == null)
            { throw new NFSGeneralException("File not found: " + fileFullName); }

            SetAttributeArguments dpArgSAttr = new SetAttributeArguments();

            dpArgSAttr.Handle = new NFSHandle(Attributes.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
            dpArgSAttr.Attributes = new MakeAttributes();
            dpArgSAttr.Attributes.LastAccessedTime = new NFSTimeValue();
            dpArgSAttr.Attributes.ModifiedTime = new NFSTimeValue();
            dpArgSAttr.Attributes.Mode = Attributes.Mode;
            dpArgSAttr.Attributes.UserID = -1;
            dpArgSAttr.Attributes.GroupID = -1;
            dpArgSAttr.Attributes.Size = size;
            dpArgSAttr.GuardCreateTime = new NFSTimeValue();
            dpArgSAttr.GuardCheck = false;

            ResultObject<SetAttributeAccessOK, SetAttributeAccessFAIL> pAttrStat =
                _ProtocolV3.NFSPROC3_SETATTR(dpArgSAttr);

            if (pAttrStat == null || pAttrStat.Status != NFSStats.NFS_OK)
            {
                if (pAttrStat == null)
                { throw new NFSGeneralException("NFSPROC3_SETATTR: failure"); }

                ExceptionHelpers.ThrowException(pAttrStat.Status);
            }
        }

        /// <inheritdoc/>
        public override int Write(String fileFullName, long offset, int count, Byte[] buffer)
        {
            ValidateConnection();

            int rCount = 0;

            if (_CurrentItem != fileFullName)
            {
                NFSAttributes Attributes = GetItemAttributes(fileFullName);
                _CurrentItemHandleObject = new NFSHandle(Attributes.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
                _CurrentItem = fileFullName;
            }

            if (count < buffer.Length)
            { Array.Resize<byte>(ref buffer, count); }

            WriteArguments dpArgWrite = new WriteArguments();
            dpArgWrite.File = _CurrentItemHandleObject;
            dpArgWrite.Offset = offset;
            dpArgWrite.Count = count;
            dpArgWrite.Data = buffer;

            ResultObject<WriteAccessOK, WriteAccessFAIL> pAttrStat =
                _ProtocolV3.NFSPROC3_WRITE(dpArgWrite);

            if (pAttrStat != null)
            {
                if (pAttrStat.Status != NFSStats.NFS_OK)
                { ExceptionHelpers.ThrowException(pAttrStat.Status); }

                rCount = pAttrStat.OK.Count;
            }
            else
            { throw new NFSGeneralException("NFSPROC3_WRITE: failure"); }

            return rCount;
        }

        /// <inheritdoc/>
        public override void Move(string oldDirectoryFullName, string oldFileName, string newDirectoryFullName, string newFileName)
        {
            ValidateConnection();

            NFSAttributes? OldDirectory = GetItemAttributes(oldDirectoryFullName);
            NFSAttributes? NewDirectory = GetItemAttributes(newDirectoryFullName);

            if (OldDirectory == null || NewDirectory == null)
            { throw new NFSGeneralException("Source or destination directory not found"); }

            RenameArguments dpArgRename = new RenameArguments();
            dpArgRename.From = new ItemOperationArguments();
            dpArgRename.From.Directory = new NFSHandle(OldDirectory.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
            dpArgRename.From.Name = new Name(oldFileName);
            dpArgRename.To = new ItemOperationArguments();
            dpArgRename.To.Directory = new NFSHandle(NewDirectory.Handle, V3.RPC.NFSv3Protocol.NFS_V3);
            dpArgRename.To.Name = new Name(newFileName);

            ResultObject<RenameAccessOK, RenameAccessFAIL> pRenameRes =
                _ProtocolV3.NFSPROC3_RENAME(dpArgRename);

            if (pRenameRes == null || pRenameRes.Status != NFSStats.NFS_OK)
            {
                if (pRenameRes == null)
                { throw new NFSGeneralException("NFSPROC3_WRITE: failure"); }

                ExceptionHelpers.ThrowException(pRenameRes.Status);
            }
        }

        /// <inheritdoc/>
        public override bool IsDirectory(string directoryFullName)
        {
            ValidateConnection();

            NFSAttributes? Attributes = GetItemAttributes(directoryFullName);

            return (Attributes != null && Attributes.NFSType == NFSItemTypes.NFDIR);
        }

        /// <inheritdoc/>
        public override void CompleteIO()
        {
            _CurrentItemHandleObject = null;
            _CurrentItem = string.Empty;
        }

        /// <inheritdoc/>
        public override void CreateSymbolicLink(string linkPath, string targetPath, NFSPermission mode)
        {
            ValidateConnection();

            string linkDirectory = System.IO.Path.GetDirectoryName(linkPath) ?? ".";
            string linkName = System.IO.Path.GetFileName(linkPath);

            NFSAttributes? dirAttributes = GetItemAttributes(linkDirectory);
            if (dirAttributes == null)
            {
                throw new NFSGeneralException($"Directory not found: {linkDirectory}");
            }

            SymlinkArguments symlinkArgs = new SymlinkArguments();

            // Set up the location (where) for the symlink
            ItemOperationArguments where = new ItemOperationArguments();
            where.Directory = new NFSHandle(dirAttributes.Handle, NFSv3Protocol.NFS_V3);
            where.Name = new Name(linkName);

            // Set up the symlink data (target and attributes)
            SymlinkData symlinkData = new SymlinkData();
            MakeAttributes makeAttrs = new MakeAttributes();
            makeAttrs.Mode = mode;
            makeAttrs.SetMode = true;
            makeAttrs.UserID = _UserID;
            makeAttrs.SetUserID = true;
            makeAttrs.GroupID = _GroupID;
            makeAttrs.SetGroupID = true;

            // Use reflection to set private fields since no setters
            System.Reflection.FieldInfo? whereField = typeof(SymlinkArguments).GetField("_where", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo? symlinkField = typeof(SymlinkArguments).GetField("_symlink", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo? attrsField = typeof(SymlinkData).GetField("_symlink_attributes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo? dataField = typeof(SymlinkData).GetField("_symlink_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            attrsField?.SetValue(symlinkData, makeAttrs);
            dataField?.SetValue(symlinkData, new Name(targetPath));
            whereField?.SetValue(symlinkArgs, where);
            symlinkField?.SetValue(symlinkArgs, symlinkData);

            ResultObject<SymlinkAccessOK, SymlinkAccessFAIL> result =
                _ProtocolV3.NFSPROC3_SYMLINK(symlinkArgs);

            if (result == null || result.Status != NFSStats.NFS_OK)
            {
                if (result == null)
                {
                    throw new NFSGeneralException("NFSPROC3_SYMLINK: failure");
                }
                ExceptionHelpers.ThrowException(result.Status);
            }
        }

        /// <inheritdoc/>
        public override void CreateHardLink(string linkPath, string targetPath)
        {
            ValidateConnection();

            string linkDirectory = System.IO.Path.GetDirectoryName(linkPath) ?? ".";
            string linkName = System.IO.Path.GetFileName(linkPath);

            NFSAttributes? targetAttributes = GetItemAttributes(targetPath);
            if (targetAttributes == null)
            {
                throw new NFSGeneralException($"Target file not found: {targetPath}");
            }

            NFSAttributes? dirAttributes = GetItemAttributes(linkDirectory);
            if (dirAttributes == null)
            {
                throw new NFSGeneralException($"Directory not found: {linkDirectory}");
            }

            LinkArguments linkArgs = new LinkArguments();
            linkArgs.Handle = new NFSHandle(targetAttributes.Handle, NFSv3Protocol.NFS_V3);
            linkArgs.Link = new ItemOperationArguments();
            linkArgs.Link.Directory = new NFSHandle(dirAttributes.Handle, NFSv3Protocol.NFS_V3);
            linkArgs.Link.Name = new Name(linkName);

            ResultObject<LinkAccessOK, LinkAccessFAIL> result =
                _ProtocolV3.NFSPROC3_LINK(linkArgs);

            if (result == null || result.Status != NFSStats.NFS_OK)
            {
                if (result == null)
                {
                    throw new NFSGeneralException("NFSPROC3_LINK: failure");
                }
                ExceptionHelpers.ThrowException(result.Status);
            }
        }

        /// <inheritdoc/>
        public override string ReadSymbolicLink(string linkPath)
        {
            ValidateConnection();

            NFSAttributes? linkAttributes = GetItemAttributes(linkPath);
            if (linkAttributes == null)
            {
                throw new NFSGeneralException($"Symbolic link not found: {linkPath}");
            }

            ReadLinkArguments readLinkArgs = new ReadLinkArguments();
            readLinkArgs.Handle = new NFSHandle(linkAttributes.Handle, NFSv3Protocol.NFS_V3);

            ResultObject<ReadLinkAccessOK, ReadLinkAccessFAIL> result =
                _ProtocolV3.NFSPROC3_READLINK(readLinkArgs);

            if (result == null || result.Status != NFSStats.NFS_OK)
            {
                if (result == null)
                {
                    throw new NFSGeneralException("NFSPROC3_READLINK: failure");
                }
                ExceptionHelpers.ThrowException(result.Status);
            }

            return result.OK.Name.Value;
        }

        #endregion Public Methods
    }
}