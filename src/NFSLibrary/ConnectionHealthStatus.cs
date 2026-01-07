namespace NFSLibrary
{
    /// <summary>
    /// Represents the health status of an NFS connection.
    /// </summary>
    public enum ConnectionHealthStatus
    {
        /// <summary>
        /// Health status is unknown (no check performed yet).
        /// </summary>
        Unknown,

        /// <summary>
        /// Connection is healthy and responsive.
        /// </summary>
        Healthy,

        /// <summary>
        /// Connection experienced some failures but is still functional.
        /// </summary>
        Degraded,

        /// <summary>
        /// Connection is unhealthy and likely needs reconnection.
        /// </summary>
        Unhealthy
    }
}
