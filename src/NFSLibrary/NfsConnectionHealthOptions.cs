namespace NFSLibrary
{
    using System;

    /// <summary>
    /// Options for NFS connection health monitoring.
    /// </summary>
    public class NfsConnectionHealthOptions
    {
        /// <summary>
        /// Gets or sets whether automatic heartbeat checking is enabled.
        /// Default is true.
        /// </summary>
        public bool EnableAutoHeartbeat { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval between heartbeat checks.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the number of consecutive failures before marking as unhealthy.
        /// Default is 3.
        /// </summary>
        public int UnhealthyThreshold { get; set; } = 3;

        /// <summary>
        /// Gets or sets the timeout for health check operations.
        /// Default is 10 seconds.
        /// </summary>
        public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}
