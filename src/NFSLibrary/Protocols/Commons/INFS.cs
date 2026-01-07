namespace NFSLibrary.Protocols.Commons
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    /// <summary>
    /// Interface defining the NFS protocol operations supported by this library.
    /// </summary>
    public interface INFS : IDisposable
    {
        /// <summary>
        /// Establishes a connection to an NFS server.
        /// </summary>
        /// <param name="address">The IP address of the NFS server.</param>
        /// <param name="userId">The Unix user ID for authentication.</param>
        /// <param name="groupId">The Unix group ID for authentication.</param>
        /// <param name="clientTimeout">The timeout for client operations in milliseconds.</param>
        /// <param name="characterEncoding">The character encoding to use for file names.</param>
        /// <param name="useSecurePort">If true, uses a local binding port less than 1024.</param>
        /// <param name="useFhCache">If true, enables file handle caching.</param>
        /// <param name="nfsPort">The NFS server port. Use 0 to discover via portmapper.</param>
        /// <param name="mountPort">The mount protocol port (NFSv2/v3 only). Use 0 to discover via portmapper.</param>
        void Connect(IPAddress address, int userId, int groupId, int clientTimeout, System.Text.Encoding characterEncoding, bool useSecurePort, bool useFhCache, int nfsPort = 0, int mountPort = 0);

        /// <summary>
        /// Closes the connection to the NFS server.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Gets the maximum block size for read/write operations.
        /// </summary>
        /// <returns>The block size in bytes.</returns>
        int GetBlockSize();

        /// <summary>
        /// Gets the list of exported devices (shares) from the NFS server.
        /// </summary>
        /// <returns>A list of exported device paths.</returns>
        List<String> GetExportedDevices();

        /// <summary>
        /// Mounts an NFS export.
        /// </summary>
        /// <param name="deviceName">The name of the device/export to mount.</param>
        void MountDevice(String deviceName);

        /// <summary>
        /// Unmounts the currently mounted NFS export.
        /// </summary>
        void UnMountDevice();

        /// <summary>
        /// Gets the list of items (files and directories) in a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory.</param>
        /// <returns>A list of item names in the directory.</returns>
        List<String> GetItemList(String directoryFullName);

        /// <summary>
        /// Gets the attributes of a file or directory.
        /// </summary>
        /// <param name="itemFullName">The full path of the item.</param>
        /// <param name="throwExceptionIfNotFound">If true, throws an exception when the item is not found.</param>
        /// <returns>The attributes of the item, or null if not found and throwExceptionIfNotFound is false.</returns>
        NFSAttributes? GetItemAttributes(String itemFullName, bool throwExceptionIfNotFound);

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory to create.</param>
        /// <param name="mode">The permissions to set on the new directory.</param>
        void CreateDirectory(String directoryFullName, NFSPermission mode);

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path of the directory to delete.</param>
        void DeleteDirectory(String directoryFullName);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to delete.</param>
        void DeleteFile(String fileFullName);

        /// <summary>
        /// Creates a new file.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to create.</param>
        /// <param name="mode">The permissions to set on the new file.</param>
        void CreateFile(String fileFullName, NFSPermission mode);

        /// <summary>
        /// Reads data from a file.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to read.</param>
        /// <param name="offset">The byte offset to start reading from.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="buffer">The buffer to store the read data.</param>
        /// <returns>The actual number of bytes read.</returns>
        int Read(String fileFullName, long offset, int count, ref byte[] buffer);

        /// <summary>
        /// Sets the size of a file.
        /// </summary>
        /// <param name="fileFullName">The full path of the file.</param>
        /// <param name="size">The new size in bytes.</param>
        void SetFileSize(String fileFullName, long size);

        /// <summary>
        /// Writes data to a file.
        /// </summary>
        /// <param name="fileFullName">The full path of the file to write.</param>
        /// <param name="offset">The byte offset to start writing at.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="buffer">The buffer containing the data to write.</param>
        /// <returns>The actual number of bytes written.</returns>
        int Write(String fileFullName, long offset, int count, byte[] buffer);

        /// <summary>
        /// Moves or renames a file.
        /// </summary>
        /// <param name="oldDirectoryFullName">The source directory path.</param>
        /// <param name="oldFileName">The source file name.</param>
        /// <param name="newDirectoryFullName">The destination directory path.</param>
        /// <param name="newFileName">The destination file name.</param>
        void Move(String oldDirectoryFullName, String oldFileName, String newDirectoryFullName, String newFileName);

        /// <summary>
        /// Determines whether the specified path refers to a directory.
        /// </summary>
        /// <param name="directoryFullName">The full path to check.</param>
        /// <returns>True if the path is a directory; otherwise, false.</returns>
        bool IsDirectory(String directoryFullName);

        /// <summary>
        /// Completes any pending I/O operations and releases resources.
        /// </summary>
        void CompleteIO();

        /// <summary>
        /// Creates a symbolic link.
        /// </summary>
        /// <param name="linkPath">The full path of the symbolic link to create.</param>
        /// <param name="targetPath">The path that the symbolic link points to.</param>
        /// <param name="mode">The permissions to set on the symbolic link.</param>
        void CreateSymbolicLink(string linkPath, string targetPath, NFSPermission mode);

        /// <summary>
        /// Creates a hard link.
        /// </summary>
        /// <param name="linkPath">The full path of the hard link to create.</param>
        /// <param name="targetPath">The path of the existing file to link to.</param>
        void CreateHardLink(string linkPath, string targetPath);

        /// <summary>
        /// Reads the target of a symbolic link.
        /// </summary>
        /// <param name="linkPath">The full path of the symbolic link to read.</param>
        /// <returns>The target path that the symbolic link points to.</returns>
        string ReadSymbolicLink(string linkPath);
    }
}
