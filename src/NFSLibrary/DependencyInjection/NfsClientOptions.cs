#if NET8_0_OR_GREATER
namespace NFSLibrary.DependencyInjection
{
    /// <summary>
    /// Options for configuring an NFS client.
    /// </summary>
    public class NfsClientOptions
    {
        /// <summary>
        /// Gets or sets the name of this configuration (for named clients).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the IP address or hostname of the NFS server.
        /// </summary>
        public string? ServerAddress { get; set; }

        /// <summary>
        /// Gets or sets the NFS version to use.
        /// </summary>
        public NfsVersion Version { get; set; } = NfsVersion.V3;

        /// <summary>
        /// Gets or sets the Unix user ID for authentication.
        /// </summary>
        public int UserId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the Unix group ID for authentication.
        /// </summary>
        public int GroupId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the client timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 60000;

        /// <summary>
        /// Gets or sets whether to use a secure port (less than 1024).
        /// </summary>
        public bool UseSecurePort { get; set; }

        /// <summary>
        /// Gets or sets whether to use file handle caching.
        /// </summary>
        public bool UseFhCache { get; set; } = true;

        /// <summary>
        /// Gets or sets the default export to mount on connection.
        /// </summary>
        public string? DefaultExport { get; set; }
    }
}
#endif
