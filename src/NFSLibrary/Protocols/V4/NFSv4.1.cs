namespace NFSLibrary.Protocols.V4
{
    using NFSLibrary.Protocols.Commons;
    using NFSLibrary.Protocols.Commons.Exceptions;
    using NFSLibrary.Protocols.V4.RPC;
    using NFSLibrary.Protocols.V4.RPC.Stubs;
    using NFSLibrary.Rpc;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Timers;

    /// <summary>
    /// Implements the NFSv4.1 protocol for network file system operations.
    /// </summary>
    public class NFSv4 : INFS
    {
        #region Fields

        private NFSv4ProtocolClient _ProtocolV4 = null;

        // private String _MountedDevice = String.Empty;
        private String _CurrentItem = String.Empty;

        private int _GroupID = -1;
        private int _UserID = -1;

        private NfsFh4 _RootFH = null;

        private Clientid4 _ClientIdByServer = null;
        private Sequenceid4 _SequenceID = null;
        private Sessionid4 _SessionId = null;

        private bool _UseFHCache = false;
        private FileHandleCache? _FileHandleCache = null;

        //create _Timerz
        private Timer _Timer = null;

        //**************back to the roots*********************

        //current dir handle
        //private NfsFh4 _cwd = null;
        //current file handle  (read ,write)
        private NfsFh4 _Cwf = null;

        //before write dir handle and other
        //private NfsFh4 _cWwf = null;
        //private string _beforeWritePath = null;

        private Stateid4 _CurrentState = null;
        //private string _currentFolder;

        //private List<NfsFh4>  _cwhTree;
        //private int treePosition = 0;

        private long _LastUpdate = -1;

        private int _MaxTRrate;
        private int _MaxRXrate;

        private bool _Disposed = false;

        #endregion Fields

        #region Constructur

        /// <summary>
        /// Establishes a connection to an NFSv4.1 server.
        /// </summary>
        /// <param name="address">The IP address of the NFS server.</param>
        /// <param name="userId">The user ID for authentication.</param>
        /// <param name="groupId">The group ID for authentication.</param>
        /// <param name="clientTimeout">The timeout in milliseconds for client operations. Uses 60000ms if set to 0.</param>
        /// <param name="characterEncoding">The character encoding to use. Uses ASCII if null.</param>
        /// <param name="useSecurePort">Whether to use a secure port for communication.</param>
        /// <param name="useFhCache">Whether to enable file handle caching for improved performance.</param>
        /// <param name="nfsPort">The NFS server port. Use 0 to discover via portmapper, or 2049 for standard NFSv4 port.</param>
        /// <param name="mountPort">Ignored for NFSv4 (no mount protocol).</param>
        public void Connect(IPAddress address, int userId, int groupId, int clientTimeout, System.Text.Encoding characterEncoding, bool useSecurePort, bool useFhCache, int nfsPort = 0, int mountPort = 0)
        {
            if (clientTimeout == 0)
            { clientTimeout = 60000; }

            if (characterEncoding == null)
            { characterEncoding = System.Text.Encoding.ASCII; }

            _CurrentItem = String.Empty;

            _UseFHCache = useFhCache;

            if (_UseFHCache)
                _FileHandleCache = new FileHandleCache(
                    defaultExpiration: TimeSpan.FromSeconds(30),
                    enableAutoCleanup: true);

            _RootFH = null;

            //_cwd = null;

            _Cwf = null;

            //_cwhTree = new List<NfsFh4>();
            //treePosition = 0;

            _GroupID = groupId;
            _UserID = userId;

            // Use specified NFS port if provided, otherwise let portmapper discover it
            if (nfsPort > 0)
            {
                _ProtocolV4 = new NFSv4ProtocolClient(address, nfsPort, OncRpcProtocols.ONCRPC_TCP);
            }
            else
            {
                _ProtocolV4 = new NFSv4ProtocolClient(address, OncRpcProtocols.ONCRPC_TCP, useSecurePort);
            }

            OncRpcClientAuthUnix authUnix = new OncRpcClientAuthUnix(address.ToString(), userId, groupId);

            _ProtocolV4.GetClient().SetAuth(authUnix);
            _ProtocolV4.GetClient().SetTimeout(clientTimeout);
            _ProtocolV4.GetClient().SetCharacterEncoding(characterEncoding.WebName);

            //send null dummy procedure to see if server is responding
            SendNullProcedure();
        }

        #endregion Constructur

        #region Public Methods

        /// <summary>
        /// Disconnects from the NFSv4.1 server and releases session resources.
        /// </summary>
        public void Disconnect()
        {
            //_RootDirectoryHandleObject = null;
            //_CurrentItemHandleObject = null;

            //_MountedDevice = String.Empty;
            _CurrentItem = String.Empty;

            if (_ProtocolV4 != null)
            {
                //new thingy in nfs 4.1
                DestroySession();

                if (_Timer != null)
                    _Timer.Close();

                //not supported in major linux distibutions
                //DestroyClientId();
                _ProtocolV4.Close();
            }
        }

        /// <summary>
        /// Gets the optimal block size for read and write operations based on server capabilities.
        /// </summary>
        /// <returns>The block size in bytes, capped at 65236 bytes.</returns>
        public int GetBlockSize()
        {
            if (_MaxTRrate > 7900 && _MaxRXrate > 7900)
            {
                if (_MaxTRrate > 65236 && _MaxRXrate > 65236)
                    return 65236;
                else if (_MaxTRrate == _MaxRXrate)
                    return _MaxTRrate - 200;
                else if (_MaxTRrate < _MaxRXrate)
                    return _MaxTRrate - 200;
                else
                    return _MaxRXrate - 200;
            }
            else
                return 7900;
        }

        /// <summary>
        /// Gets the list of exported file systems from the NFS server.
        /// </summary>
        /// <returns>A list of exported device paths. NFSv4.1 returns only the root "/" as a single export.</returns>
        public List<String> GetExportedDevices()
        {
            List<string> nfsDevices = new List<string>();
            nfsDevices.Add("/");

            // Only create session if not already established
            // Calling mount() again would create a new session which can cause issues
            if (_SessionId == null)
            {
                Mount();
            }

            return nfsDevices;
        }

        /// <summary>
        /// Mounts the specified NFS device.
        /// </summary>
        /// <param name="deviceName">The name of the device to mount.</param>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected.</exception>
        public void MountDevice(String deviceName)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            // Ensure session is established (NFSv4.1 requirement)
            if (_SessionId == null)
            {
                Mount();
            }

            GetRootFh();
            ReclaimComplete();
        }

        /// <summary>
        /// Unmounts the currently mounted NFS device.
        /// </summary>
        public void UnMountDevice()
        {
            //maybe we need something to do here also
        }

        /// <summary>
        /// Retrieves a list of items (files and directories) in the specified directory.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory. Use "." for the root directory.</param>
        /// <returns>A list of item names in the directory.</returns>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected.</exception>
        /// <exception cref="NFSGeneralException">Thrown when access is denied or other NFS errors occur.</exception>
        public List<String> GetItemList(String directoryFullName)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            /*if (DirectoryFullName == ".")
            {
                //root
                _cwd = _RootFH;
                _cwhTree.Add(_cwd);
            }
            else if (_currentFolder == DirectoryFullName)
            {
                //do nothing just recheck folder
            }
            else if (DirectoryFullName.IndexOf(_currentFolder) != -1)
            {
                //one level up

                NFSAttributes itemAttributes = GetItemAttributes(DirectoryFullName);
                _cwd = itemAttributes.fh;
                _cwhTree.Add(_cwd);
                treePosition++;
            }
            else
            {
                //level down
                _cwhTree.RemoveAt(treePosition);
                treePosition--;
                _cwd =_cwhTree[treePosition];
            }*/

            //_currentFolder = directoryFullName;
            if (String.IsNullOrEmpty(directoryFullName) || directoryFullName == ".")
                return GetItemListByFH(_RootFH);

            //simpler way than tree
            NFSAttributes itemAttributes = GetItemAttributes(directoryFullName);

            //true dat
            return GetItemListByFH(new NfsFh4(itemAttributes.Handle));
        }

        /// <summary>
        /// Retrieves a list of items in a directory using a file handle.
        /// </summary>
        /// <param name="dir_fh">The file handle of the directory.</param>
        /// <returns>A list of item names in the directory.</returns>
        /// <exception cref="NFSGeneralException">Thrown when access is denied or other NFS errors occur.</exception>
        public List<String> GetItemListByFH(NfsFh4 dir_fh)
        {
            //should return result
            int access = GetFileHandleAccess(dir_fh);

            List<string> ItemsList = new List<string>();

            //has read access
            if (access % 2 == 1)
            {
                bool done = false;
                long cookie = 0;

                Verifier4 verifier = new Verifier4(0);

                do
                {
                    List<NfsArgop4> ops = new List<NfsArgop4>();
                    ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                            _SequenceID.Value.Value, 12, 0));
                    ops.Add(PutfhStub.GenerateRequest(dir_fh));
                    ops.Add(ReadDirStub.GenerateRequest(cookie, verifier));

                    Compound4Res compound4res = SendCompound(ops, "");

                    if (compound4res.Status == Nfsstat4.NFS4_OK)
                    {
                        verifier = compound4res.Resarray[2].Opreaddir.Resok4.Cookieverf;
                        done = compound4res.Resarray[2].Opreaddir.Resok4.Reply.Eof;

                        Entry4 dirEntry = compound4res.Resarray[2].Opreaddir.Resok4.Reply.Entries;
                        while (dirEntry != null)
                        {
                            cookie = dirEntry.Cookie.Value.Value;
                            string name = System.Text.Encoding.UTF8.GetString(dirEntry.Name.Value.Value.Value);
                            ItemsList.Add(name);
                            dirEntry = dirEntry.Nextentry;
                        }
                    }
                    else
                    {
                        throw new NFSGeneralException(Nfsstat4.getErrorString(compound4res.Status));
                    }
                } while (!done);
                //now do the lookups (maintained by the nfsclient)
            }
            else
                throw new NFSGeneralException(Nfsstat4.getErrorString(Nfsstat4.NFS4ERR_ACCESS));

            return ItemsList;
        }

        /// <summary>
        /// Retrieves the attributes of a file or directory.
        /// </summary>
        /// <param name="itemFullName">The full path of the item.</param>
        /// <param name="throwExceptionIfNotFound">Whether to throw an exception if the item is not found. Default is true.</param>
        /// <returns>The attributes of the item, or null if not found and throwExceptionIfNotFound is false.</returns>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or other connection errors occur.</exception>
        public NFSAttributes GetItemAttributes(string itemFullName, bool throwExceptionIfNotFound = true)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            itemFullName = itemFullName.Replace(".\\.\\", ".\\");

            if (_UseFHCache && _FileHandleCache != null)
            {
                NFSAttributes? cachedAttrs = _FileHandleCache.Get(itemFullName);
                if (cachedAttrs != null)
                    return cachedAttrs;
            }

            //we will return it in the old way !! ;)
            NFSAttributes attributes = null;

            if (String.IsNullOrEmpty(itemFullName))
            {
                //should not happen
                return attributes;
            }

            // Handle root directory (both "." and ".\.")
            if (itemFullName == "." || itemFullName == ".\\.")
                return new NFSAttributes(0, 0, 0, NFSItemTypes.NFDIR, new NFSPermission(7, 7, 7), 4096, _RootFH.Value);

            NfsFh4 currentItem = _RootFH;
            int initial = 1;
            String[] PathTree = itemFullName.Split(@"\".ToCharArray());

            if (_UseFHCache && _FileHandleCache != null)
            {
                string parent = System.IO.Path.GetDirectoryName(itemFullName);
                //get cached parent dir to avoid too much directory
                if (parent != itemFullName)
                {
                    NFSAttributes? parentAttrs = _FileHandleCache.Get(parent);
                    if (parentAttrs != null)
                    {
                        currentItem.Value = parentAttrs.Handle;
                        initial = PathTree.Length - 1;
                    }
                }
            }

            for (int pC = initial; pC < PathTree.Length; pC++)
            {
                List<int> attrs = new List<int>(1);
                attrs.Add(NFSv4Protocol.FATTR4_TIME_CREATE);
                attrs.Add(NFSv4Protocol.FATTR4_TIME_ACCESS);
                attrs.Add(NFSv4Protocol.FATTR4_TIME_MODIFY);
                attrs.Add(NFSv4Protocol.FATTR4_TYPE);
                attrs.Add(NFSv4Protocol.FATTR4_MODE);
                attrs.Add(NFSv4Protocol.FATTR4_SIZE);

                List<NfsArgop4> ops = new List<NfsArgop4>();

                ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                        _SequenceID.Value.Value, 12, 0));

                ops.Add(PutfhStub.GenerateRequest(currentItem));
                ops.Add(LookupStub.GenerateRequest(PathTree[pC]));

                //ops.Add(PutfhStub.GenerateRequest(_cwd));
                //ops.Add(LookupStub.GenerateRequest(PathTree[PathTree.Length-1]));

                ops.Add(GetfhStub.GenerateRequest());
                ops.Add(GetattrStub.GenerateRequest(attrs));

                Compound4Res compound4res = SendCompound(ops, "");

                if (compound4res.Status == Nfsstat4.NFS4_OK)
                {
                    currentItem = compound4res.Resarray[3].Opgetfh.Resok4.Object1;

                    //NfsFh4 currentItem = compound4res.Resarray[3].Opgetfh.Resok4.Object1;

                    //results
                    Dictionary<int, Object> attrrs_results = GetattrStub.DecodeType(compound4res.Resarray[4].Opgetattr.Resok4.Obj_attributes);

                    //times
                    Nfstime4 time_acc = (Nfstime4)attrrs_results[NFSv4Protocol.FATTR4_TIME_ACCESS];

                    int time_acc_int = unchecked((int)time_acc.Seconds.Value);

                    Nfstime4 time_modify = (Nfstime4)attrrs_results[NFSv4Protocol.FATTR4_TIME_MODIFY];

                    int time_modif = unchecked((int)time_modify.Seconds.Value);

                    int time_creat = 0;
                    //linux should now store create time if it is let's check it else use modify date
                    if (attrrs_results.ContainsKey(NFSv4Protocol.FATTR4_TIME_CREATE))
                    {
                        Nfstime4 time_create = (Nfstime4)attrrs_results[NFSv4Protocol.FATTR4_TIME_CREATE];

                        time_creat = unchecked((int)time_create.Seconds.Value);
                    }
                    else
                        time_creat = time_modif;

                    //3 = type
                    NFSItemTypes nfstype = NFSItemTypes.NFREG;

                    Fattr4Type type = (Fattr4Type)attrrs_results[NFSv4Protocol.FATTR4_TYPE];

                    if (type.Value == 2)
                        nfstype = NFSItemTypes.NFDIR;

                    //4 = mode is int also
                    Mode4 mode = (Mode4)attrrs_results[NFSv4Protocol.FATTR4_MODE];

                    byte other = (byte)(mode.Value.Value % 8);

                    byte grup = (byte)((mode.Value.Value >> 3) % 8);

                    byte user = (byte)((mode.Value.Value >> 6) % 8);

                    NFSPermission per = new NFSPermission(user, grup, other);

                    Uint64T size = (Uint64T)attrrs_results[NFSv4Protocol.FATTR4_SIZE];
                    //here we do attributes compatible with old nfs versions
                    attributes = new NFSAttributes(time_creat, time_acc_int, time_modif, nfstype, per, size.Value, currentItem.Value);
                }
                else if (compound4res.Status == Nfsstat4.NFS4ERR_NOENT)
                {
                    return null;
                }
                else
                {
                    throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
                }
            }

            // if(attributes.NFSType == NFSItemTypes.NFDIR)
            if (_UseFHCache && _FileHandleCache != null)
                _FileHandleCache.Set(itemFullName, attributes);

            return attributes;
        }

        /// <summary>
        /// Creates a new directory on the NFS server.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory to create.</param>
        /// <param name="mode">The permissions to set on the directory. If null, defaults to 777.</param>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or creation fails.</exception>
        public void CreateDirectory(string directoryFullName, NFSPermission mode)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            int user = 7;
            int group = 7;
            int other = 7;

            if (mode != null)
            {
                user = mode.UserAccess;
                group = mode.GroupAccess;
                other = mode.OtherAccess;
            }

            string ParentDirectory = System.IO.Path.GetDirectoryName(directoryFullName);
            string fileName = System.IO.Path.GetFileName(directoryFullName);
            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            //create item attributes now
            Fattr4 attr = new Fattr4();

            attr.Attrmask = OpenStub.OpenFattrBitmap();
            attr.Attr_vals = new Attrlist4();
            attr.Attr_vals.Value = OpenStub.OpenAttrs(user, group, other, 4096);

            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            ops.Add(PutfhStub.GenerateRequest(new NfsFh4(ParentItemAttributes.Handle)));
            ops.Add(CreateStub.GenerateRequest(fileName, attr));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                //create directory ok
            }
            else { throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status)); }
        }

        /// <summary>
        /// Deletes a directory from the NFS server.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory to delete.</param>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or deletion fails.</exception>
        public void DeleteDirectory(string directoryFullName)
        {
            //nfs 4.1 now uses same support for folders and files
            DeleteFile(directoryFullName);
        }

        /// <summary>
        /// Deletes a file from the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to delete.</param>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or deletion fails.</exception>
        public void DeleteFile(string fileFullName)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            // it seems access doesn't work ok in the server
            //delete access isn't showed but file can be deleted and should be deleted!
            /*NFSAttributes atrs = GetItemAttributes(fileFullName);

          int access = GetFileHandleAccess(atrs.fh);

             //delete support
          if ((access >> 4) % 2 == 1)
          {*/

            string ParentDirectory = System.IO.Path.GetDirectoryName(fileFullName);
            string fileName = System.IO.Path.GetFileName(fileFullName);

            NFSAttributes ParentItemAttributes = GetItemAttributes(ParentDirectory);

            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            ops.Add(PutfhStub.GenerateRequest(new NfsFh4(ParentItemAttributes.Handle)));
            ops.Add(RemoveStub.GenerateRequest(fileName));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                //ok - deleted
            }
            else { throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status)); }

            /* }
             else
             {
                 throw new NFSGeneralException("Acess Denied");
             }*/

            //only if we support caching
            if (_UseFHCache && _FileHandleCache != null)
                _FileHandleCache.Invalidate(fileFullName);
        }

        /// <summary>
        /// Creates a new file on the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to create.</param>
        /// <param name="mode">The permissions to set on the file.</param>
        /// <exception cref="NFSConnectionException">Thrown when creation fails.</exception>
        public void CreateFile(string fileFullName, NFSPermission mode)
        {
            if (_CurrentItem != fileFullName)
            {
                _CurrentItem = fileFullName;

                String[] PathTree = fileFullName.Split(@"\".ToCharArray());

                string ParentDirectory = System.IO.Path.GetDirectoryName(fileFullName);
                NFSAttributes ParentAttributes = GetItemAttributes(ParentDirectory);

                //make open here
                List<NfsArgop4> ops = new List<NfsArgop4>();
                ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
            _SequenceID.Value.Value, 12, 0));
                //dir  herez
                ops.Add(PutfhStub.GenerateRequest(new NfsFh4(ParentAttributes.Handle)));
                // NFSv4.1 (RFC 5661): open_owner seqid should be 0 for new sessions
                ops.Add(OpenStub.NormalCREATE(PathTree[PathTree.Length - 1], 0, _ClientIdByServer, NFSv4Protocol.OPEN4_SHARE_ACCESS_WRITE));
                ops.Add(GetfhStub.GenerateRequest());

                Compound4Res compound4res = SendCompound(ops, "");
                if (compound4res.Status == Nfsstat4.NFS4_OK)
                {
                    //open ok
                    _CurrentState = compound4res.Resarray[2].Opopen.Resok4.Stateid;

                    _Cwf = compound4res.Resarray[3].Opgetfh.Resok4.Object1;
                }
                else
                {
                    throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
                }
            }
        }

        /// <summary>
        /// Reads data from a file on the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to read.</param>
        /// <param name="offset">The byte offset in the file to start reading from.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="buffer">The buffer to store the read data.</param>
        /// <returns>The actual number of bytes read.</returns>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected, read access is denied, or read fails.</exception>
        public int Read(String fileFullName, long offset, int count, ref Byte[] buffer)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            int rCount = 0;

            if (count == 0)
                return 0;

            if (_CurrentItem != fileFullName)
            {
                NFSAttributes Attributes = GetItemAttributes(fileFullName);
                _Cwf = new NfsFh4(Attributes.Handle);
                _CurrentItem = fileFullName;

                string ParentDirectory = System.IO.Path.GetDirectoryName(fileFullName);
                NFSAttributes ParentAttributes = GetItemAttributes(ParentDirectory);

                String[] PathTree = fileFullName.Split(@"\".ToCharArray());

                //make open here
                List<NfsArgop4> ops = new List<NfsArgop4>();
                ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
            _SequenceID.Value.Value, 12, 0));
                //dir  herez
                //ops.Add(PutfhStub.GenerateRequest(_cwd));
                ops.Add(PutfhStub.GenerateRequest(new NfsFh4(ParentAttributes.Handle)));
                //let's try with sequence 0
                ops.Add(OpenStub.NormalREAD(PathTree[PathTree.Length - 1], 0, _ClientIdByServer, NFSv4Protocol.OPEN4_SHARE_ACCESS_READ));

                Compound4Res compound4res = SendCompound(ops, "");
                if (compound4res.Status == Nfsstat4.NFS4_OK)
                {
                    //open ok
                    _CurrentState = compound4res.Resarray[2].Opopen.Resok4.Stateid;
                }
                else
                {
                    throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
                }

                //check the access also
                if (GetFileHandleAccess(_Cwf) % 2 != 1)
                {
                    //we don't have read access give error
                    throw new NFSConnectionException("Sorry no file READ access !!!");
                }
            }

            List<NfsArgop4> ops2 = new List<NfsArgop4>();
            ops2.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
        _SequenceID.Value.Value, 12, 0));
            ops2.Add(PutfhStub.GenerateRequest(_Cwf));
            ops2.Add(ReadStub.GenerateRequest(count, offset, _CurrentState));

            Compound4Res compound4res2 = SendCompound(ops2, "");
            if (compound4res2.Status == Nfsstat4.NFS4_OK)
            {
                //read of offset complete
                rCount = compound4res2.Resarray[2].Opread.Resok4.Data.Length;

                //copy the data to the output
                Array.Copy(compound4res2.Resarray[2].Opread.Resok4.Data, buffer, rCount);
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res2.Status));
            }

            return rCount;
        }

        private void CloseFile(NfsFh4 fh, Stateid4 stateid)
        {
            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));

            ops.Add(PutfhStub.GenerateRequest(fh));
            ops.Add(CloseStub.GenerateRequest(stateid));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                //close file ok
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        /// <summary>
        /// Sets the size of a file on the NFS server, truncating or extending it as needed.
        /// </summary>
        /// <param name="fileFullName">The full path of the file.</param>
        /// <param name="size">The new size in bytes.</param>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or the operation fails.</exception>
        public void SetFileSize(string fileFullName, long size)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            NFSAttributes attributes = GetItemAttributes(fileFullName);
            NfsFh4 fileHandle = new NfsFh4(attributes.Handle);

            string parentDirectory = System.IO.Path.GetDirectoryName(fileFullName);
            NFSAttributes parentAttributes = GetItemAttributes(parentDirectory);
            String[] pathTree = fileFullName.Split(@"\".ToCharArray());

            // Open the file for writing to get a stateid
            List<NfsArgop4> openOps = new List<NfsArgop4>();
            openOps.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            openOps.Add(PutfhStub.GenerateRequest(new NfsFh4(parentAttributes.Handle)));
            // NFSv4.1 (RFC 5661): open_owner seqid should be 0 for new sessions
            openOps.Add(OpenStub.NormalOPENonly(pathTree[pathTree.Length - 1], 0, _ClientIdByServer, NFSv4Protocol.OPEN4_SHARE_ACCESS_WRITE));
            openOps.Add(GetfhStub.GenerateRequest());

            Compound4Res openRes = SendCompound(openOps, "");
            if (openRes.Status != Nfsstat4.NFS4_OK)
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(openRes.Status));
            }

            Stateid4 openStateid = openRes.Resarray[2].Opopen.Resok4.Stateid;
            NfsFh4 openedFileHandle = openRes.Resarray[3].Opgetfh.Resok4.Object1;

            // Set the file size
            List<NfsArgop4> setAttrOps = new List<NfsArgop4>();
            setAttrOps.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            setAttrOps.Add(PutfhStub.GenerateRequest(openedFileHandle));
            setAttrOps.Add(SetAttrStub.GenerateSetSizeRequest(openStateid, size));

            Compound4Res setAttrRes = SendCompound(setAttrOps, "");

            // Close the file
            CloseFile(openedFileHandle, openStateid);

            if (setAttrRes.Status != Nfsstat4.NFS4_OK)
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(setAttrRes.Status));
            }

            // Invalidate cache entry if caching is enabled
            if (_UseFHCache && _FileHandleCache != null)
            {
                _FileHandleCache.Invalidate(fileFullName);
            }

            // Invalidate current file state so next Write will re-open the file
            if (_CurrentItem == fileFullName)
            {
                _CurrentItem = String.Empty;
                _CurrentState = null;
                _Cwf = null;
            }
        }

        /// <summary>
        /// Writes data to a file on the NFS server.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to write to.</param>
        /// <param name="offset">The byte offset in the file to start writing at.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="buffer">The buffer containing the data to write.</param>
        /// <returns>The actual number of bytes written.</returns>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or write fails.</exception>
        public int Write(String fileFullName, long offset, int count, Byte[] buffer)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            int rCount = 0;
            //NfsFh4 current = _cwd;

            if (_CurrentItem != fileFullName)
            {
                _CurrentItem = fileFullName;

                String[] PathTree = fileFullName.Split(@"\".ToCharArray());

                string ParentDirectory = System.IO.Path.GetDirectoryName(fileFullName);
                NFSAttributes ParentAttributes = GetItemAttributes(ParentDirectory);

                //make open here
                List<NfsArgop4> ops = new List<NfsArgop4>();
                ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
            _SequenceID.Value.Value, 12, 0));
                //dir  herez
                ops.Add(PutfhStub.GenerateRequest(new NfsFh4(ParentAttributes.Handle)));
                // NFSv4.1 (RFC 5661): open_owner seqid should be 0 for new sessions
                ops.Add(OpenStub.NormalOPENonly(PathTree[PathTree.Length - 1], 0, _ClientIdByServer, NFSv4Protocol.OPEN4_SHARE_ACCESS_WRITE));
                ops.Add(GetfhStub.GenerateRequest());

                Compound4Res compound4res = SendCompound(ops, "");
                if (compound4res.Status == Nfsstat4.NFS4_OK)
                {
                    //open ok
                    _CurrentState = compound4res.Resarray[2].Opopen.Resok4.Stateid;

                    _Cwf = compound4res.Resarray[3].Opgetfh.Resok4.Object1;
                }
                else
                {
                    throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
                }
            }

            List<NfsArgop4> ops2 = new List<NfsArgop4>();
            ops2.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
        _SequenceID.Value.Value, 12, 0));
            ops2.Add(PutfhStub.GenerateRequest(_Cwf));

            //make better buffer
            Byte[] Buffer2 = new Byte[count];
            Array.Copy(buffer, Buffer2, count);
            ops2.Add(WriteStub.GenerateRequest(offset, Buffer2, _CurrentState));

            Compound4Res compound4res2 = SendCompound(ops2, "");
            if (compound4res2.Status == Nfsstat4.NFS4_OK)
            {
                //write of offset complete
                rCount = compound4res2.Resarray[2].Opwrite.Resok4.Count.Value.Value;
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res2.Status));
            }

            return rCount;
        }

        /// <summary>
        /// Moves or renames a file or directory on the NFS server.
        /// </summary>
        /// <param name="oldDirectoryFullName">The full path of the source directory.</param>
        /// <param name="oldFileName">The name of the file or directory to move/rename.</param>
        /// <param name="newDirectoryFullName">The full path of the destination directory.</param>
        /// <param name="newFileName">The new name for the file or directory.</param>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or the operation fails.</exception>
        public void Move(string oldDirectoryFullName, string oldFileName, string newDirectoryFullName, string newFileName)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            NFSAttributes oldDirectory = GetItemAttributes(oldDirectoryFullName);
            NFSAttributes newDirectory = GetItemAttributes(newDirectoryFullName);

            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            ops.Add(PutfhStub.GenerateRequest(new NfsFh4(oldDirectory.Handle)));
            ops.Add(SavefhStub.GenerateRequest());
            ops.Add(PutfhStub.GenerateRequest(new NfsFh4(newDirectory.Handle)));
            ops.Add(RenameStub.GenerateRequest(oldFileName, newFileName));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                // Rename successful
                // Invalidate cache entries if caching is enabled
                if (_UseFHCache && _FileHandleCache != null)
                {
                    string oldPath = System.IO.Path.Combine(oldDirectoryFullName, oldFileName);
                    string newPath = System.IO.Path.Combine(newDirectoryFullName, newFileName);
                    _FileHandleCache.Invalidate(oldPath);
                    _FileHandleCache.Invalidate(newPath);
                }
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        /// <summary>
        /// Determines whether the specified path is a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path to check.</param>
        /// <returns>True if the path is a directory; otherwise, false.</returns>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected.</exception>
        public bool IsDirectory(string directoryFullName)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            NFSAttributes Attributes = GetItemAttributes(directoryFullName);

            return (Attributes != null && Attributes.NFSType == NFSItemTypes.NFDIR);
        }

        /// <summary>
        /// Completes any pending I/O operations and closes the currently open file.
        /// </summary>
        public void CompleteIO()
        {
            if (_Cwf != null || _CurrentItem != string.Empty)
            {
                CloseFile(_Cwf, _CurrentState);
                _CurrentItem = string.Empty;
                _Cwf = null;
            }
        }

        //part of new nfsv4.1 functions
        private void Mount()
        {
            //tell own id and ask server for id he want's we to use it
            ExchangeIds();
            //create session!
            CreateSession();

            //we created session ok  now let's start the _Timer
            _Timer = new Timer(10000);
            _Timer.Elapsed += new ElapsedEventHandler(TimerCallback);
            _Timer.Enabled = true; // Enable it
        }

        private void ReclaimComplete()
        {
            List<NfsArgop4> ops = new List<NfsArgop4>();
            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
        _SequenceID.Value.Value, 12, 0));
            ops.Add(ReclaimCompleteStub.GenerateRequest(false));

            Compound4Res compound4res = SendCompound(ops, "");
            if (compound4res.Status != Nfsstat4.NFS4_OK)
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        private void GetRootFh()
        {
            List<int> attrs = new List<int>(1);
            attrs.Add(NFSv4Protocol.FATTR4_LEASE_TIME);
            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 0, 0));
            ops.Add(PutrootfhStub.GenerateRequest());
            ops.Add(GetfhStub.GenerateRequest());
            ops.Add(GetattrStub.GenerateRequest(attrs));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                _RootFH = compound4res.Resarray[2].Opgetfh.Resok4.Object1;
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        /// <summary>
        /// Converts a byte array to a hexadecimal string representation.
        /// </summary>
        /// <param name="data">The byte array to convert.</param>
        /// <returns>A string containing the hexadecimal representation of the bytes.</returns>
        public String ToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in data)
            {
                sb.Append(b.ToString("X"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the access permissions for a file handle.
        /// </summary>
        /// <param name="file_handle">The file handle to check.</param>
        /// <returns>An integer representing access permissions (bitfield).</returns>
        /// <exception cref="NFSConnectionException">Thrown when the access check fails.</exception>
        public int GetFileHandleAccess(NfsFh4 file_handle)
        {
            Uint32T access = new Uint32T(0);

            //all acsses possible
            access.Value = access.Value +
            NFSv4Protocol.ACCESS4_READ +
            NFSv4Protocol.ACCESS4_LOOKUP +
            NFSv4Protocol.ACCESS4_MODIFY +
            NFSv4Protocol.ACCESS4_EXTEND +
            NFSv4Protocol.ACCESS4_DELETE; //+
            //NFSv4Protocol.ACCESS4_EXECUTE;

            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            ops.Add(PutfhStub.GenerateRequest(file_handle));
            ops.Add(AcessStub.GenerateRequest(access));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                return compound4res.Resarray[2].Opaccess.Resok4.Access.Value;
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        private void ExchangeIds()
        {
            List<NfsArgop4> ops = new List<NfsArgop4>();

            String domain = "localhost";
            String name = "NFS Client ";

            //String guid = System.Environment.MachineName + "@" + domain;
            String guid = System.Guid.NewGuid().ToString();

            ops.Add(ExchengeIDStub.Normal(domain, name, guid, NFSv4Protocol.EXCHGID4_FLAG_SUPP_MOVED_REFER + NFSv4Protocol.EXCHGID4_FLAG_USE_NON_PNFS, StateProtectHow4.SP4_NONE));

            Compound4Res compound4res = SendCompound(ops, "");
            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                /*if (compound4res.Resarray[0].Opexchange_id.Eir_resok4.eir_server_impl_id.Length > 0)
                {
                    string serverId = System.Text.Encoding.UTF8.GetString(compound4res.Resarray[0].Opexchange_id.Eir_resok4.eir_server_impl_id[0].nii_name.Value.Value);
                }
                else
                {
                    if (compound4res.Resarray[0].Opexchange_id.Eir_resok4.eir_server_owner.so_major_id.Length > 0)
                    {
                        string serverId = System.Text.Encoding.UTF8.GetString(compound4res.Resarray[0].Opexchange_id.Eir_resok4.eir_server_owner.so_major_id);
                        //throw new NFSConnectionException("Server name: ="+serverId);
                    }
                }*/

                _ClientIdByServer = compound4res.Resarray[0].Opexchange_id.Eir_resok4.Eir_clientid;
                _SequenceID = compound4res.Resarray[0].Opexchange_id.Eir_resok4.Eir_sequenceid;
            }
            else { throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status)); }
        }

        private void CreateSession()
        {
            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(CreateSessionStub.Standard(_ClientIdByServer, _SequenceID));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                _SessionId = compound4res.Resarray[0].Opcreate_session.Csr_resok4.Csr_sessionid;
                // NFSv4.1 requires sequence ID to start at 0 after session creation (RFC 5661 Section 18.36)
                _SequenceID.Value.Value = 0;

                _MaxTRrate = compound4res.Resarray[0].Opcreate_session.Csr_resok4.Csr_fore_chan_attrs.Ca_maxrequestsize.Value.Value;
                _MaxRXrate = compound4res.Resarray[0].Opcreate_session.Csr_resok4.Csr_fore_chan_attrs.Ca_maxresponsesize.Value.Value;
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        // a very nice to send a client null procedure
        private void SendNullProcedure()
        {
            _ProtocolV4.NFSPROC4_NULL_4();
        }

        private void DestroySession()
        {
            if (_SessionId != null)
            {
                List<NfsArgop4> ops = new List<NfsArgop4>();

                ops.Add(DestroySessionStub.Standard(_SessionId));

                Compound4Res compound4res = SendCompound(ops, "");
                _SessionId = null;
            }
        }

        private void DestroyClientId()
        {
            if (_ClientIdByServer != null)
            {
                List<NfsArgop4> ops = new List<NfsArgop4>();

                ops.Add(DestroyClientIdStub.Standard(_ClientIdByServer));

                Compound4Res compound4res = SendCompound(ops, "");
                _ClientIdByServer = null;
            }
        }

        private Compound4Res SendCompound(List<NfsArgop4> ops, String tag)
        {
            Compound4Res compound4res;
            Compound4Args compound4args = generateCompound(tag, ops);

            // Wait if server is in the grace period (retry on NFS4ERR_GRACE or NFS4ERR_DELAY)
            // Grace period is typically 10-90 seconds after server restart, depending on server config.
            // We retry for up to 100 seconds which should cover most grace periods.
            int retryCount = 0;
            const int maxRetries = 100; // ~100 seconds max wait for grace period
            do
            {
                compound4res = _ProtocolV4.NFSPROC4_COMPOUND_4(compound4args);
                ProcessSequence(compound4res, compound4args);

                if (compound4res.Status == Nfsstat4.NFS4ERR_GRACE || compound4res.Status == Nfsstat4.NFS4ERR_DELAY)
                {
                    if (++retryCount > maxRetries)
                    {
                        throw new NFSConnectionException($"Server grace period exceeded maximum wait time ({maxRetries} retries). Status: {compound4res.Status}");
                    }
                    System.Threading.Thread.Sleep(1000); // Wait 1 second before retry
                }
            } while (compound4res.Status == Nfsstat4.NFS4ERR_GRACE || compound4res.Status == Nfsstat4.NFS4ERR_DELAY);

            return compound4res;
        }

        /// <summary>
        /// Generates a COMPOUND4 arguments structure for NFSv4.1 operations.
        /// </summary>
        /// <param name="tag">A tag string to identify the compound operation.</param>
        /// <param name="opList">The list of NFS operations to include in the compound request.</param>
        /// <returns>A Compound4Args structure ready to send to the server.</returns>
        public static Compound4Args generateCompound(String tag,
        List<NfsArgop4> opList)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] bytes = encoding.GetBytes(tag);

            Compound4Args compound4args = new Compound4Args();
            compound4args.Tag = new Utf8strCs(new Utf8string(bytes));
            compound4args.Minorversion = new Uint32T(1);

            //compound4args.Argarray = opList.ToArray(new NfsArgop4[opList.Count]);
            compound4args.Argarray = opList.ToArray();

            return compound4args;
        }

        /// <summary>
        /// Processes the sequence operation response to update sequence IDs and track connection health.
        /// </summary>
        /// <param name="compound4res">The compound response from the server.</param>
        /// <param name="compound4args">The compound arguments sent to the server.</param>
        public void ProcessSequence(Compound4Res compound4res, Compound4Args compound4args)
        {
            if (compound4res.Resarray != null && compound4res.Resarray.Length != 0 && compound4res.Resarray[0].Resop == NfsOpnum4.OP_SEQUENCE &&
                    compound4res.Resarray[0].Opsequence.SrStatus == Nfsstat4.NFS4_OK)
            {
                _LastUpdate = GetGMTInMS();
                ++_SequenceID.Value.Value;
                // Increment the compound's sequence ID for retry on DELAY or GRACE
                // The server consumed the sequence ID, so we need to use the next one
                if (compound4res.Status == Nfsstat4.NFS4ERR_DELAY || compound4res.Status == Nfsstat4.NFS4ERR_GRACE)
                    compound4args.Argarray[0].Opsequence.SaSequenceid.Value.Value++;
            }
        }

        private void SendOnlySequence()
        {
            List<NfsArgop4> ops = new List<NfsArgop4>();
            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
        _SequenceID.Value.Value, 12, 0));

            Compound4Res compound4res = SendCompound(ops, "");
            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                //reclaim complete
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        private void TimerCallback(object sender, ElapsedEventArgs e)
        {
            if (NeedUpdate())
                SendOnlySequence();
        }

        /// <summary>
        /// Gets the current GMT time in milliseconds since the Unix epoch (January 1, 1970).
        /// </summary>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public static long GetGMTInMS()
        {
            TimeSpan unixTime = DateTime.Now.ToUniversalTime() -
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return (long)unixTime.TotalMilliseconds;
        }

        /// <summary>
        /// Determines if a sequence update is needed to maintain the session.
        /// </summary>
        /// <returns>True if more than 59 seconds have passed since the last update; otherwise, false.</returns>
        private bool NeedUpdate()
        {
            // 60 seconds
            return (GetGMTInMS() - _LastUpdate) > 59000;
        }

        /// <summary>
        /// Creates a symbolic link on the NFS server.
        /// </summary>
        /// <param name="linkPath">The path where the symbolic link will be created.</param>
        /// <param name="targetPath">The path that the symbolic link will point to.</param>
        /// <param name="mode">The permissions for the symbolic link.</param>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or creation fails.</exception>
        public void CreateSymbolicLink(string linkPath, string targetPath, NFSPermission mode)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            int user = 7;
            int group = 7;
            int other = 7;

            if (mode != null)
            {
                user = mode.UserAccess;
                group = mode.GroupAccess;
                other = mode.OtherAccess;
            }

            string parentDirectory = System.IO.Path.GetDirectoryName(linkPath);
            string linkName = System.IO.Path.GetFileName(linkPath);
            NFSAttributes parentItemAttributes = GetItemAttributes(parentDirectory);

            // Create file attributes
            Fattr4 attr = new Fattr4();
            attr.Attrmask = OpenStub.OpenFattrBitmap();
            attr.Attr_vals = new Attrlist4();
            attr.Attr_vals.Value = OpenStub.OpenAttrs(user, group, other, 0);

            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            ops.Add(PutfhStub.GenerateRequest(new NfsFh4(parentItemAttributes.Handle)));
            ops.Add(SymlinkStub.GenerateRequest(linkName, targetPath, attr));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                // Symbolic link created successfully
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        /// <summary>
        /// Creates a hard link on the NFS server.
        /// </summary>
        /// <param name="linkPath">The path where the hard link will be created.</param>
        /// <param name="targetPath">The path of the existing file to link to.</param>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or creation fails.</exception>
        /// <exception cref="NFSGeneralException">Thrown when the target file does not exist.</exception>
        public void CreateHardLink(string linkPath, string targetPath)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            // Get the target file's attributes (and validate it exists)
            NFSAttributes targetAttributes = GetItemAttributes(targetPath);
            if (targetAttributes == null)
            {
                throw new NFSGeneralException("Target file does not exist: " + targetPath);
            }

            // Get the parent directory for the new link
            string linkParentDirectory = System.IO.Path.GetDirectoryName(linkPath);
            string linkName = System.IO.Path.GetFileName(linkPath);
            NFSAttributes linkParentAttributes = GetItemAttributes(linkParentDirectory);

            List<NfsArgop4> ops = new List<NfsArgop4>();

            // Build compound: SEQUENCE -> PUTFH(target) -> SAVEFH -> PUTFH(linkParent) -> LINK(linkName)
            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            ops.Add(PutfhStub.GenerateRequest(new NfsFh4(targetAttributes.Handle)));
            ops.Add(SavefhStub.GenerateRequest());
            ops.Add(PutfhStub.GenerateRequest(new NfsFh4(linkParentAttributes.Handle)));
            ops.Add(LinkStub.GenerateRequest(linkName));

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                // Hard link created successfully
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        /// <summary>
        /// Reads the target path of a symbolic link.
        /// </summary>
        /// <param name="linkPath">The path of the symbolic link to read.</param>
        /// <returns>The target path that the symbolic link points to.</returns>
        /// <exception cref="NFSConnectionException">Thrown when the NFS client is not connected or the operation fails.</exception>
        /// <exception cref="NFSGeneralException">Thrown when the symbolic link does not exist.</exception>
        public string ReadSymbolicLink(string linkPath)
        {
            if (_ProtocolV4 == null)
            { throw new NFSConnectionException("NFS Client not connected!"); }

            NFSAttributes linkAttributes = GetItemAttributes(linkPath);
            if (linkAttributes == null)
            {
                throw new NFSGeneralException("Symbolic link does not exist: " + linkPath);
            }

            List<NfsArgop4> ops = new List<NfsArgop4>();

            ops.Add(SequenceStub.GenerateRequest(false, _SessionId.Value,
                    _SequenceID.Value.Value, 12, 0));
            ops.Add(PutfhStub.GenerateRequest(new NfsFh4(linkAttributes.Handle)));
            ops.Add(ReadLinkStub.GenerateRequest());

            Compound4Res compound4res = SendCompound(ops, "");

            if (compound4res.Status == Nfsstat4.NFS4_OK)
            {
                // Extract the link target from the response
                // Response array: [0]=SEQUENCE, [1]=PUTFH, [2]=READLINK
                Readlink4Resok readlinkResult = compound4res.Resarray[2].Opreadlink.Resok4;
                byte[] linkData = readlinkResult.Link.Value.Value.Value;
                return System.Text.Encoding.UTF8.GetString(linkData);
            }
            else
            {
                throw new NFSConnectionException(Nfsstat4.getErrorString(compound4res.Status));
            }
        }

        #endregion Public Methods

        #region IDisposable

        /// <summary>
        /// Releases all resources used by the NFSv4 instance.
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
                _FileHandleCache?.Dispose();
            }

            _Disposed = true;
        }

        #endregion IDisposable
    }
}