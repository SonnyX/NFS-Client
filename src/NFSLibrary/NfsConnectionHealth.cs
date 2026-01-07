namespace NFSLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Monitors NFS connection health and provides heartbeat functionality.
    /// </summary>
    public sealed class NfsConnectionHealth : IDisposable
    {
        private readonly NfsClient _Client;
        private readonly NfsConnectionHealthOptions _Options;
        private readonly Timer? _HeartbeatTimer;
        private readonly object _Lock = new object();

        private bool _Disposed;
        private DateTime _LastSuccessfulCheck;
        private int _ConsecutiveFailures;
        private ConnectionHealthStatus _CurrentStatus;

        /// <summary>
        /// Occurs when the connection health status changes.
        /// </summary>
        public event EventHandler<ConnectionHealthChangedEventArgs>? HealthStatusChanged;

        /// <summary>
        /// Gets the current health status of the connection.
        /// </summary>
        public ConnectionHealthStatus Status
        {
            get
            {
                lock (_Lock)
                {
                    return _CurrentStatus;
                }
            }
        }

        /// <summary>
        /// Gets the time of the last successful health check.
        /// </summary>
        public DateTime LastSuccessfulCheck
        {
            get
            {
                lock (_Lock)
                {
                    return _LastSuccessfulCheck;
                }
            }
        }

        /// <summary>
        /// Gets the number of consecutive failed health checks.
        /// </summary>
        public int ConsecutiveFailures
        {
            get
            {
                lock (_Lock)
                {
                    return _ConsecutiveFailures;
                }
            }
        }

        /// <summary>
        /// Creates a new connection health monitor for the specified client.
        /// </summary>
        /// <param name="client">The NFS client to monitor.</param>
        /// <param name="options">Health monitoring options.</param>
        public NfsConnectionHealth(NfsClient client, NfsConnectionHealthOptions? options = null)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Options = options ?? new NfsConnectionHealthOptions();
            _LastSuccessfulCheck = DateTime.UtcNow;
            _CurrentStatus = ConnectionHealthStatus.Unknown;

            if (_Options.EnableAutoHeartbeat)
            {
                _HeartbeatTimer = new Timer(
                    HeartbeatCallback,
                    null,
                    _Options.HeartbeatInterval,
                    _Options.HeartbeatInterval);
            }
        }

        /// <summary>
        /// Performs a health check on the connection.
        /// </summary>
        /// <returns>The health check result.</returns>
        public HealthCheckResult CheckHealth()
        {
            DateTime startTime = DateTime.UtcNow;

            try
            {
                // Try to get the list of exports as a health check
                // This is a lightweight operation that verifies connectivity
                List<string> exports = _Client.GetExportedDevices();

                TimeSpan latency = DateTime.UtcNow - startTime;

                lock (_Lock)
                {
                    _LastSuccessfulCheck = DateTime.UtcNow;
                    _ConsecutiveFailures = 0;
                    UpdateStatus(ConnectionHealthStatus.Healthy);
                }

                return new HealthCheckResult(
                    isHealthy: true,
                    latency: latency,
                    message: $"Connection healthy. Found {exports.Count} exports.");
            }
            catch (Exception ex)
            {
                TimeSpan latency = DateTime.UtcNow - startTime;

                lock (_Lock)
                {
                    _ConsecutiveFailures++;

                    if (_ConsecutiveFailures >= _Options.UnhealthyThreshold)
                    {
                        UpdateStatus(ConnectionHealthStatus.Unhealthy);
                    }
                    else if (_CurrentStatus == ConnectionHealthStatus.Healthy)
                    {
                        UpdateStatus(ConnectionHealthStatus.Degraded);
                    }
                }

                return new HealthCheckResult(
                    isHealthy: false,
                    latency: latency,
                    message: $"Health check failed: {ex.Message}",
                    exception: ex);
            }
        }

        /// <summary>
        /// Performs a health check asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(CheckHealth, cancellationToken);
        }

        /// <summary>
        /// Attempts to reconnect if the connection is unhealthy.
        /// </summary>
        /// <returns>True if reconnection was successful or not needed.</returns>
        public bool TryReconnect()
        {
            if (_CurrentStatus == ConnectionHealthStatus.Healthy)
                return true;

            try
            {
                _Client.Disconnect();
            }
            catch
            {
                // Ignore disconnect errors
            }

            try
            {
                // Note: Reconnection requires the original connection parameters
                // The client would need to store these for full reconnection support
                HealthCheckResult result = CheckHealth();
                return result.IsHealthy;
            }
            catch
            {
                return false;
            }
        }

        private void HeartbeatCallback(object? state)
        {
            if (_Disposed) return;

            try
            {
                CheckHealth();
            }
            catch
            {
                // Suppress exceptions in timer callback
            }
        }

        private void UpdateStatus(ConnectionHealthStatus newStatus)
        {
            if (_CurrentStatus != newStatus)
            {
                ConnectionHealthStatus oldStatus = _CurrentStatus;
                _CurrentStatus = newStatus;

                HealthStatusChanged?.Invoke(this, new ConnectionHealthChangedEventArgs(oldStatus, newStatus));
            }
        }

        /// <summary>
        /// Disposes of the health monitor.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;

            _Disposed = true;
            _HeartbeatTimer?.Dispose();
        }
    }
}
