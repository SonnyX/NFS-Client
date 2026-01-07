namespace NFSLibrary
{
    using System;
    using System.Text;
    /// <summary>
    /// Configuration options for NFS client connections.
    /// </summary>
    public class NfsConnectionOptions
    {
        /// <summary>
        /// Gets or sets the Unix user ID for authentication.
        /// </summary>
        /// <remarks>Default is 0 (root).</remarks>
        public int UserId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the Unix group ID for authentication.
        /// </summary>
        /// <remarks>Default is 0 (root).</remarks>
        public int GroupId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the command timeout in milliseconds.
        /// </summary>
        /// <remarks>Default is 60000ms (60 seconds).</remarks>
        public int CommandTimeoutMs { get; set; } = 60000;

        /// <summary>
        /// Gets or sets the command timeout as a TimeSpan.
        /// </summary>
        /// <remarks>This is an alternative to CommandTimeoutMs for more readable configuration.</remarks>
        public TimeSpan CommandTimeout
        {
            get => TimeSpan.FromMilliseconds(CommandTimeoutMs);
            set => CommandTimeoutMs = (int)value.TotalMilliseconds;
        }

        /// <summary>
        /// Gets or sets the character encoding for file names.
        /// </summary>
        /// <remarks>Default is ASCII encoding.</remarks>
        public Encoding CharacterEncoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// Gets or sets whether to use a secure port (less than 1024) for the local binding.
        /// </summary>
        /// <remarks>
        /// Default is true. Some NFS servers require connections from privileged ports.
        /// Setting this to true may require elevated privileges on the client.
        /// </remarks>
        public bool UseSecurePort { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable file handle caching.
        /// </summary>
        /// <remarks>
        /// Default is false. When enabled, file handles are cached to reduce
        /// round trips to the server. This can improve performance but may
        /// cause issues if files are modified by other clients.
        /// </remarks>
        public bool UseFileHandleCache { get; set; } = false;

        /// <summary>
        /// Gets or sets the NFS server port.
        /// </summary>
        /// <remarks>
        /// Default is 0, which means use the portmapper to discover the port.
        /// For NFSv4, the standard port is 2049. Set this to a non-zero value
        /// to connect directly without using the portmapper.
        /// </remarks>
        public int NfsPort { get; set; } = 0;

        /// <summary>
        /// Gets or sets the mount protocol port (NFSv2/v3 only).
        /// </summary>
        /// <remarks>
        /// Default is 0, which means use the portmapper to discover the port.
        /// Set this to a non-zero value to connect directly without using the portmapper.
        /// This is ignored for NFSv4 which doesn't use a separate mount protocol.
        /// </remarks>
        public int MountPort { get; set; } = 0;

        /// <summary>
        /// Creates a new instance of NfsConnectionOptions with default values.
        /// </summary>
        public NfsConnectionOptions()
        {
        }

        /// <summary>
        /// Creates a copy of the current options.
        /// </summary>
        /// <returns>A new NfsConnectionOptions instance with the same values.</returns>
        public NfsConnectionOptions Clone()
        {
            return new NfsConnectionOptions
            {
                UserId = this.UserId,
                GroupId = this.GroupId,
                CommandTimeoutMs = this.CommandTimeoutMs,
                CharacterEncoding = this.CharacterEncoding,
                UseSecurePort = this.UseSecurePort,
                UseFileHandleCache = this.UseFileHandleCache,
                NfsPort = this.NfsPort,
                MountPort = this.MountPort
            };
        }

        /// <summary>
        /// Creates default options suitable for anonymous access.
        /// </summary>
        public static NfsConnectionOptions Default => new NfsConnectionOptions();

        /// <summary>
        /// Creates options configured for a specific user.
        /// </summary>
        /// <param name="userId">The Unix user ID.</param>
        /// <param name="groupId">The Unix group ID.</param>
        /// <returns>A new NfsConnectionOptions instance.</returns>
        public static NfsConnectionOptions ForUser(int userId, int groupId)
        {
            return new NfsConnectionOptions
            {
                UserId = userId,
                GroupId = groupId
            };
        }
    }
}
