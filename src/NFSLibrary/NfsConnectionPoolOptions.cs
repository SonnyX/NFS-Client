namespace NFSLibrary
{
    using System;

    /// <summary>
    /// Configuration options for the NFS connection pool.
    /// </summary>
    public sealed class NfsConnectionPoolOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of connections per server/device combination.
        /// Default is 10.
        /// </summary>
        public int MaxPoolSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the idle timeout after which connections are removed from the pool.
        /// Default is 5 minutes.
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets whether automatic maintenance is enabled.
        /// Default is true.
        /// </summary>
        public bool EnableMaintenance { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval between maintenance runs.
        /// Default is 1 minute.
        /// </summary>
        public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromMinutes(1);
    }
}
