namespace NFSLibrary.Protocols.Commons
{
    using NFSLibrary.Protocols.Commons.Exceptions;
    using NFSLibrary.Protocols.Commons.Exceptions.Mount;
    using System;
    /// <summary>
    /// Abstract base class for NFS protocol implementations.
    /// Provides common fields, validation methods, and path handling utilities
    /// shared across NFSv2, NFSv3, and NFSv4 implementations.
    /// </summary>
    public abstract class NFSProtocolBase : INFS
    {
        #region Fields

        /// <summary>
        /// Handle to the root directory of the mounted export.
        /// </summary>
        protected NFSHandle? _RootDirectoryHandleObject;

        /// <summary>
        /// Handle to the currently referenced item.
        /// </summary>
        protected NFSHandle? _CurrentItemHandleObject;

        /// <summary>
        /// The name of the currently mounted device/export.
        /// </summary>
        protected string _MountedDevice = string.Empty;

        /// <summary>
        /// The current item path being referenced.
        /// </summary>
        protected string _CurrentItem = string.Empty;

        /// <summary>
        /// The Unix group ID for authentication.
        /// </summary>
        protected int _GroupID = -1;

        /// <summary>
        /// The Unix user ID for authentication.
        /// </summary>
        protected int _UserID = -1;

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        protected bool _Disposed = false;

        #endregion Fields

        #region Validation Methods

        /// <summary>
        /// Validates that the protocol client is connected.
        /// </summary>
        /// <exception cref="NFSConnectionException">Thrown when not connected.</exception>
        protected void ValidateProtocolConnection()
        {
            if (!IsProtocolClientConnected())
            {
                throw new NFSConnectionException("NFS Client not connected!");
            }
        }

        /// <summary>
        /// Validates that the mount protocol client is connected.
        /// </summary>
        /// <exception cref="NFSMountConnectionException">Thrown when not connected.</exception>
        protected void ValidateMountConnection()
        {
            if (!IsMountProtocolClientConnected())
            {
                throw new NFSMountConnectionException("NFS Device not connected!");
            }
        }

        /// <summary>
        /// Validates both protocol and mount connections.
        /// </summary>
        protected void ValidateConnection()
        {
            ValidateProtocolConnection();
            ValidateMountConnection();
        }

        /// <summary>
        /// Checks if the protocol client is connected. Must be implemented by derived classes.
        /// </summary>
        protected abstract bool IsProtocolClientConnected();

        /// <summary>
        /// Checks if the mount protocol client is connected. Must be implemented by derived classes.
        /// </summary>
        protected abstract bool IsMountProtocolClientConnected();

        #endregion Validation Methods

        #region Path Utilities

        /// <summary>
        /// Parses a path string into its component parts.
        /// </summary>
        /// <param name="path">The path to parse.</param>
        /// <returns>An array of path components.</returns>
        protected static string[] ParsePathComponents(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new[] { "." };
            }

            return path.Split('\\');
        }

        /// <summary>
        /// Gets the parent directory path from a full path.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <returns>The parent directory path, or "." if at root.</returns>
        protected static string GetParentDirectory(string fullPath)
        {
            string? parent = System.IO.Path.GetDirectoryName(fullPath);
            return string.IsNullOrEmpty(parent) ? "." : parent;
        }

        /// <summary>
        /// Gets the item name (file or directory name) from a full path.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <returns>The item name.</returns>
        protected static string GetItemName(string fullPath)
        {
            return System.IO.Path.GetFileName(fullPath);
        }

        /// <summary>
        /// Normalizes a path, defaulting empty paths to ".".
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        protected static string NormalizePath(string? path)
        {
            return string.IsNullOrEmpty(path) ? "." : path;
        }

        #endregion Path Utilities

        #region Connection State

        /// <summary>
        /// Resets connection state fields to their initial values.
        /// Called during connect and disconnect operations.
        /// </summary>
        protected virtual void ResetConnectionState()
        {
            _RootDirectoryHandleObject = null;
            _CurrentItemHandleObject = null;
            _MountedDevice = string.Empty;
            _CurrentItem = string.Empty;
        }

        /// <summary>
        /// Resets mount state fields.
        /// Called during unmount operations.
        /// </summary>
        protected virtual void ResetMountState()
        {
            _RootDirectoryHandleObject = null;
            _CurrentItemHandleObject = null;
            _MountedDevice = string.Empty;
            _CurrentItem = string.Empty;
        }

        #endregion Connection State

        #region Abstract Methods (INFS implementation)

        /// <inheritdoc/>
        public abstract void Connect(System.Net.IPAddress address, int userId, int groupId, int clientTimeout,
            System.Text.Encoding characterEncoding, bool useSecurePort, bool useFhCache, int nfsPort = 0, int mountPort = 0);

        /// <inheritdoc/>
        public abstract void Disconnect();

        /// <inheritdoc/>
        public abstract int GetBlockSize();

        /// <inheritdoc/>
        public abstract System.Collections.Generic.List<string> GetExportedDevices();

        /// <inheritdoc/>
        public abstract void MountDevice(string deviceName);

        /// <inheritdoc/>
        public abstract void UnMountDevice();

        /// <inheritdoc/>
        public abstract System.Collections.Generic.List<string> GetItemList(string directoryFullName);

        /// <inheritdoc/>
        public abstract NFSAttributes? GetItemAttributes(string itemFullName, bool throwExceptionIfNotFound);

        /// <inheritdoc/>
        public abstract void CreateDirectory(string directoryFullName, NFSPermission mode);

        /// <inheritdoc/>
        public abstract void DeleteDirectory(string directoryFullName);

        /// <inheritdoc/>
        public abstract void DeleteFile(string fileFullName);

        /// <inheritdoc/>
        public abstract void CreateFile(string fileFullName, NFSPermission mode);

        /// <inheritdoc/>
        public abstract int Read(string fileFullName, long offset, int count, ref byte[] buffer);

        /// <inheritdoc/>
        public abstract void SetFileSize(string fileFullName, long size);

        /// <inheritdoc/>
        public abstract int Write(string fileFullName, long offset, int count, byte[] buffer);

        /// <inheritdoc/>
        public abstract void Move(string oldDirectoryFullName, string oldFileName,
            string newDirectoryFullName, string newFileName);

        /// <inheritdoc/>
        public abstract bool IsDirectory(string directoryFullName);

        /// <inheritdoc/>
        public abstract void CompleteIO();

        /// <inheritdoc/>
        public abstract void CreateSymbolicLink(string linkPath, string targetPath, NFSPermission mode);

        /// <inheritdoc/>
        public abstract void CreateHardLink(string linkPath, string targetPath);

        /// <inheritdoc/>
        public abstract string ReadSymbolicLink(string linkPath);

        #endregion Abstract Methods

        #region IDisposable

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by this instance.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(); false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            if (disposing)
            {
                DisposeProtocolClients();
            }

            _Disposed = true;
        }

        /// <summary>
        /// Disposes protocol client resources. Must be implemented by derived classes.
        /// </summary>
        protected abstract void DisposeProtocolClients();

        #endregion IDisposable
    }
}
