namespace NFSLibrary
{
    using System;

    /// <summary>
    /// Represents the result of a health check.
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Gets whether the health check passed.
        /// </summary>
        public bool IsHealthy { get; }

        /// <summary>
        /// Gets the latency of the health check operation.
        /// </summary>
        public TimeSpan Latency { get; }

        /// <summary>
        /// Gets a message describing the health check result.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the exception if the health check failed, or null if successful.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Creates a new health check result.
        /// </summary>
        /// <param name="isHealthy">Whether the health check passed.</param>
        /// <param name="latency">The latency of the health check operation.</param>
        /// <param name="message">A message describing the health check result.</param>
        /// <param name="exception">The exception if the health check failed, or null if successful.</param>
        public HealthCheckResult(bool isHealthy, TimeSpan latency, string message, Exception? exception = null)
        {
            IsHealthy = isHealthy;
            Latency = latency;
            Message = message;
            Exception = exception;
        }
    }
}
