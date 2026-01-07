namespace NFSLibrary.Protocols.V4
{
    using NFSLibrary.Protocols.Commons;
    using NFSLibrary.Protocols.V4.RPC;
    using NFSLibrary.Protocols.V4.RPC.Stubs;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A fluent builder for constructing NFSv4 compound operations.
    /// Allows batching multiple operations into a single network request for improved efficiency.
    /// </summary>
    public sealed class CompoundOperationBuilder
    {
        private readonly List<NfsArgop4> _Operations = new();
        private readonly Sessionid4 _SessionId;
        private readonly Sequenceid4 _SequenceId;
        private readonly Clientid4 _ClientId;
        private string _Tag = "";

        /// <summary>
        /// Gets the number of operations currently in the builder.
        /// </summary>
        public int OperationCount => _Operations.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundOperationBuilder"/> class.
        /// </summary>
        /// <param name="sessionId">The current session ID.</param>
        /// <param name="sequenceId">The current sequence ID.</param>
        /// <param name="clientId">The client ID.</param>
        public CompoundOperationBuilder(Sessionid4 sessionId, Sequenceid4 sequenceId, Clientid4 clientId)
        {
            _SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            _SequenceId = sequenceId ?? throw new ArgumentNullException(nameof(sequenceId));
            _ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        /// <summary>
        /// Sets the tag for this compound operation (for debugging/tracing).
        /// </summary>
        /// <param name="tag">The operation tag.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder WithTag(string tag)
        {
            _Tag = tag ?? "";
            return this;
        }

        /// <summary>
        /// Adds a SEQUENCE operation (required as first operation in NFSv4.1+).
        /// </summary>
        /// <param name="cachethis">Whether to cache this result.</param>
        /// <param name="slotId">The slot ID to use.</param>
        /// <param name="highestSlot">The highest slot ID.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Sequence(bool cachethis = false, int slotId = 12, int highestSlot = 0)
        {
            _Operations.Add(SequenceStub.GenerateRequest(cachethis, _SessionId.Value,
                _SequenceId.Value.Value, slotId, highestSlot));
            return this;
        }

        /// <summary>
        /// Adds a PUTROOTFH operation (set current file handle to root).
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder PutRootFh()
        {
            _Operations.Add(PutrootfhStub.GenerateRequest());
            return this;
        }

        /// <summary>
        /// Adds a PUTFH operation (set current file handle).
        /// </summary>
        /// <param name="fileHandle">The file handle to set as current.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder PutFh(NfsFh4 fileHandle)
        {
            if (fileHandle == null) throw new ArgumentNullException(nameof(fileHandle));
            _Operations.Add(PutfhStub.GenerateRequest(fileHandle));
            return this;
        }

        /// <summary>
        /// Adds a PUTFH operation using a byte array handle.
        /// </summary>
        /// <param name="handle">The file handle bytes.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder PutFh(byte[] handle)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            return PutFh(new NfsFh4(handle));
        }

        /// <summary>
        /// Adds a GETFH operation (get current file handle).
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder GetFh()
        {
            _Operations.Add(GetfhStub.GenerateRequest());
            return this;
        }

        /// <summary>
        /// Adds a LOOKUP operation.
        /// </summary>
        /// <param name="name">The name to look up.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Lookup(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            _Operations.Add(LookupStub.GenerateRequest(name));
            return this;
        }

        /// <summary>
        /// Adds a GETATTR operation with standard attributes.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder GetAttr()
        {
            List<int> attrs = new List<int>
            {
                NFSv4Protocol.FATTR4_TIME_CREATE,
                NFSv4Protocol.FATTR4_TIME_ACCESS,
                NFSv4Protocol.FATTR4_TIME_MODIFY,
                NFSv4Protocol.FATTR4_TYPE,
                NFSv4Protocol.FATTR4_MODE,
                NFSv4Protocol.FATTR4_SIZE
            };
            _Operations.Add(GetattrStub.GenerateRequest(attrs));
            return this;
        }

        /// <summary>
        /// Adds a GETATTR operation with custom attributes.
        /// </summary>
        /// <param name="attributes">The attribute IDs to request.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder GetAttr(IEnumerable<int> attributes)
        {
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            _Operations.Add(GetattrStub.GenerateRequest(attributes.ToList()));
            return this;
        }

        /// <summary>
        /// Adds an ACCESS operation to check access permissions.
        /// </summary>
        /// <param name="accessMask">The access bits to check.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Access(int accessMask = -1)
        {
            if (accessMask == -1)
            {
                accessMask = NFSv4Protocol.ACCESS4_READ +
                    NFSv4Protocol.ACCESS4_LOOKUP +
                    NFSv4Protocol.ACCESS4_MODIFY +
                    NFSv4Protocol.ACCESS4_EXTEND +
                    NFSv4Protocol.ACCESS4_DELETE;
            }
            Uint32T access = new Uint32T(accessMask);
            _Operations.Add(AcessStub.GenerateRequest(access));
            return this;
        }

        /// <summary>
        /// Adds a SAVEFH operation (save current file handle to saved FH slot).
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder SaveFh()
        {
            _Operations.Add(SavefhStub.GenerateRequest());
            return this;
        }

        /// <summary>
        /// Adds a READDIR operation.
        /// </summary>
        /// <param name="cookie">The directory cookie (0 for start).</param>
        /// <param name="verifier">The cookie verifier.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder ReadDir(long cookie = 0, Verifier4? verifier = null)
        {
            verifier ??= new Verifier4(0);
            _Operations.Add(ReadDirStub.GenerateRequest(cookie, verifier));
            return this;
        }

        /// <summary>
        /// Adds a READ operation.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="offset">The byte offset to read from.</param>
        /// <param name="stateId">The state ID from OPEN.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Read(int count, long offset, Stateid4 stateId)
        {
            if (stateId == null) throw new ArgumentNullException(nameof(stateId));
            _Operations.Add(ReadStub.GenerateRequest(count, offset, stateId));
            return this;
        }

        /// <summary>
        /// Adds a WRITE operation.
        /// </summary>
        /// <param name="offset">The byte offset to write at.</param>
        /// <param name="data">The data to write.</param>
        /// <param name="stateId">The state ID from OPEN.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Write(long offset, byte[] data, Stateid4 stateId)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (stateId == null) throw new ArgumentNullException(nameof(stateId));
            _Operations.Add(WriteStub.GenerateRequest(offset, data, stateId));
            return this;
        }

        /// <summary>
        /// Adds a CREATE operation for creating a directory.
        /// </summary>
        /// <param name="name">The directory name.</param>
        /// <param name="attr">The directory attributes.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder CreateDir(string name, Fattr4 attr)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (attr == null) throw new ArgumentNullException(nameof(attr));
            _Operations.Add(CreateStub.GenerateRequest(name, attr));
            return this;
        }

        /// <summary>
        /// Adds a REMOVE operation.
        /// </summary>
        /// <param name="name">The name of the file/directory to remove.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Remove(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            _Operations.Add(RemoveStub.GenerateRequest(name));
            return this;
        }

        /// <summary>
        /// Adds a RENAME operation (requires SAVEFH to have been called first with source dir).
        /// </summary>
        /// <param name="oldName">The current name.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Rename(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName)) throw new ArgumentNullException(nameof(oldName));
            if (string.IsNullOrEmpty(newName)) throw new ArgumentNullException(nameof(newName));
            _Operations.Add(RenameStub.GenerateRequest(oldName, newName));
            return this;
        }

        /// <summary>
        /// Adds an OPEN operation for reading.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="seqId">The sequence ID.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder OpenRead(string fileName, int seqId = 0)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            _Operations.Add(OpenStub.NormalREAD(fileName, seqId, _ClientId, NFSv4Protocol.OPEN4_SHARE_ACCESS_READ));
            return this;
        }

        /// <summary>
        /// Adds an OPEN operation for writing (opens existing file).
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="seqId">The sequence ID.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder OpenWrite(string fileName, int seqId)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            _Operations.Add(OpenStub.NormalOPENonly(fileName, seqId, _ClientId, NFSv4Protocol.OPEN4_SHARE_ACCESS_WRITE));
            return this;
        }

        /// <summary>
        /// Adds an OPEN operation that creates a new file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="seqId">The sequence ID.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder OpenCreate(string fileName, int seqId)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            _Operations.Add(OpenStub.NormalCREATE(fileName, seqId, _ClientId, NFSv4Protocol.OPEN4_SHARE_ACCESS_WRITE));
            return this;
        }

        /// <summary>
        /// Adds a CLOSE operation.
        /// </summary>
        /// <param name="stateId">The state ID from OPEN.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Close(Stateid4 stateId)
        {
            if (stateId == null) throw new ArgumentNullException(nameof(stateId));
            _Operations.Add(CloseStub.GenerateRequest(stateId));
            return this;
        }

        /// <summary>
        /// Adds a SETATTR operation to set file size (truncate).
        /// </summary>
        /// <param name="stateId">The state ID from OPEN.</param>
        /// <param name="size">The new file size.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder SetSize(Stateid4 stateId, long size)
        {
            if (stateId == null) throw new ArgumentNullException(nameof(stateId));
            _Operations.Add(SetAttrStub.GenerateSetSizeRequest(stateId, size));
            return this;
        }

        /// <summary>
        /// Adds a RECLAIM_COMPLETE operation.
        /// </summary>
        /// <param name="oneFs">Whether this is for one filesystem only.</param>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder ReclaimComplete(bool oneFs = false)
        {
            _Operations.Add(ReclaimCompleteStub.GenerateRequest(oneFs));
            return this;
        }

        /// <summary>
        /// Clears all operations from the builder.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public CompoundOperationBuilder Clear()
        {
            _Operations.Clear();
            return this;
        }

        /// <summary>
        /// Builds the compound arguments ready for sending.
        /// </summary>
        /// <returns>The Compound4Args to send.</returns>
        public Compound4Args Build()
        {
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] tagBytes = encoding.GetBytes(_Tag);

            Compound4Args args = new Compound4Args
            {
                Tag = new Utf8strCs(new Utf8string(tagBytes)),
                Minorversion = new Uint32T(1),
                Argarray = _Operations.ToArray()
            };

            return args;
        }

        /// <summary>
        /// Gets the list of operations for inspection.
        /// </summary>
        /// <returns>A read-only list of operations.</returns>
        public IReadOnlyList<NfsArgop4> GetOperations()
        {
            return _Operations.AsReadOnly();
        }

        #region Common Compound Patterns

        /// <summary>
        /// Creates a builder preconfigured for looking up a path and getting its attributes.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="sequenceId">The sequence ID.</param>
        /// <param name="clientId">The client ID.</param>
        /// <param name="parentHandle">The parent directory handle.</param>
        /// <param name="name">The name to look up.</param>
        /// <returns>A configured builder.</returns>
        public static CompoundOperationBuilder LookupWithAttributes(
            Sessionid4 sessionId,
            Sequenceid4 sequenceId,
            Clientid4 clientId,
            NfsFh4 parentHandle,
            string name)
        {
            return new CompoundOperationBuilder(sessionId, sequenceId, clientId)
                .Sequence()
                .PutFh(parentHandle)
                .Lookup(name)
                .GetFh()
                .GetAttr();
        }

        /// <summary>
        /// Creates a builder preconfigured for reading directory contents.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="sequenceId">The sequence ID.</param>
        /// <param name="clientId">The client ID.</param>
        /// <param name="dirHandle">The directory handle.</param>
        /// <param name="cookie">The directory cookie.</param>
        /// <param name="verifier">The cookie verifier.</param>
        /// <returns>A configured builder.</returns>
        public static CompoundOperationBuilder ReadDirectory(
            Sessionid4 sessionId,
            Sequenceid4 sequenceId,
            Clientid4 clientId,
            NfsFh4 dirHandle,
            long cookie = 0,
            Verifier4? verifier = null)
        {
            return new CompoundOperationBuilder(sessionId, sequenceId, clientId)
                .Sequence()
                .PutFh(dirHandle)
                .Access()
                .ReadDir(cookie, verifier);
        }

        /// <summary>
        /// Creates a builder preconfigured for moving/renaming a file.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="sequenceId">The sequence ID.</param>
        /// <param name="clientId">The client ID.</param>
        /// <param name="sourceDir">The source directory handle.</param>
        /// <param name="targetDir">The target directory handle.</param>
        /// <param name="oldName">The old file name.</param>
        /// <param name="newName">The new file name.</param>
        /// <returns>A configured builder.</returns>
        public static CompoundOperationBuilder MoveFile(
            Sessionid4 sessionId,
            Sequenceid4 sequenceId,
            Clientid4 clientId,
            NfsFh4 sourceDir,
            NfsFh4 targetDir,
            string oldName,
            string newName)
        {
            return new CompoundOperationBuilder(sessionId, sequenceId, clientId)
                .Sequence()
                .PutFh(sourceDir)
                .SaveFh()
                .PutFh(targetDir)
                .Rename(oldName, newName);
        }

        #endregion
    }
}
