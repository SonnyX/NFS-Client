namespace NFSLibrary
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A pool of NFS connections for efficient connection reuse.
    /// Supports multiple servers and automatic connection management.
    /// </summary>
    public sealed class NfsConnectionPool : IDisposable, IAsyncDisposable
    {
        private readonly ConcurrentDictionary<string, PooledConnectionCollection> _Pools = new();
        private readonly NfsConnectionPoolOptions _Options;
        private readonly ILogger _Logger;
        private readonly Timer? _MaintenanceTimer;
        private bool _Disposed;

        /// <summary>
        /// Gets the total number of connections across all pools.
        /// </summary>
        public int TotalConnections
        {
            get
            {
                int count = 0;
                foreach (PooledConnectionCollection pool in _Pools.Values)
                {
                    count += pool.TotalCount;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the number of available (idle) connections across all pools.
        /// </summary>
        public int AvailableConnections
        {
            get
            {
                int count = 0;
                foreach (PooledConnectionCollection pool in _Pools.Values)
                {
                    count += pool.AvailableCount;
                }
                return count;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NfsConnectionPool"/> class.
        /// </summary>
        /// <param name="options">The pool configuration options. If null, defaults are used.</param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        public NfsConnectionPool(NfsConnectionPoolOptions? options = null, ILogger? logger = null)
        {
            _Options = options ?? new NfsConnectionPoolOptions();
            _Logger = logger ?? NullLogger.Instance;

            if (_Options.EnableMaintenance)
            {
                _MaintenanceTimer = new Timer(
                    PerformMaintenance,
                    null,
                    _Options.MaintenanceInterval,
                    _Options.MaintenanceInterval);
            }

            _Logger.LogDebug("NFS Connection Pool initialized with MaxPoolSize={MaxPoolSize}, IdleTimeout={IdleTimeout}",
                _Options.MaxPoolSize, _Options.IdleTimeout);
        }

        /// <summary>
        /// Gets a connection from the pool, creating a new one if necessary.
        /// </summary>
        /// <param name="server">The server address.</param>
        /// <param name="device">The NFS device/export to mount.</param>
        /// <param name="version">The NFS version to use.</param>
        /// <param name="connectionOptions">Optional connection options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A pooled NFS connection.</returns>
        public async Task<PooledNfsConnection> GetConnectionAsync(
            IPAddress server,
            string device,
            NfsVersion version = NfsVersion.V3,
            NfsConnectionOptions? connectionOptions = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            string key = GetPoolKey(server, device, version);
            PooledConnectionCollection pool = _Pools.GetOrAdd(key, _ => new PooledConnectionCollection(_Options.MaxPoolSize));

            // Try to get an existing connection
            if (pool.TryDequeue(out NfsClient? existingConnection))
            {
                if (IsConnectionValid(existingConnection))
                {
                    _Logger.LogDebug("Reusing pooled connection for {Server}/{Device}", server, device);
                    return new PooledNfsConnection(existingConnection, this, key);
                }
                else
                {
                    // Connection is invalid, dispose it
                    _Logger.LogDebug("Disposing invalid pooled connection for {Server}/{Device}", server, device);
                    try { existingConnection.Disconnect(); } catch { }
                    try { existingConnection.Dispose(); } catch { }
                }
            }

            // Create a new connection
            _Logger.LogDebug("Creating new connection for {Server}/{Device}", server, device);
            NfsClient client = new NfsClient(version);

            await client.ConnectAsync(server, connectionOptions, cancellationToken).ConfigureAwait(false);
            await client.MountDeviceAsync(device, cancellationToken).ConfigureAwait(false);

            pool.IncrementTotal();
            return new PooledNfsConnection(client, this, key);
        }

        /// <summary>
        /// Gets a connection from the pool synchronously.
        /// </summary>
        /// <param name="server">The server address.</param>
        /// <param name="device">The NFS device/export to mount.</param>
        /// <param name="version">The NFS version to use.</param>
        /// <param name="connectionOptions">Optional connection options.</param>
        /// <returns>A pooled NFS connection.</returns>
        public PooledNfsConnection GetConnection(
            IPAddress server,
            string device,
            NfsVersion version = NfsVersion.V3,
            NfsConnectionOptions? connectionOptions = null)
        {
            return GetConnectionAsync(server, device, version, connectionOptions).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns a connection to the pool for reuse.
        /// </summary>
        /// <param name="client">The NFS client to return.</param>
        /// <param name="poolKey">The pool key.</param>
        internal void ReturnConnection(NfsClient client, string poolKey)
        {
            if (_Disposed)
            {
                // Pool is disposed, just clean up the connection
                try { client.Disconnect(); } catch { }
                try { client.Dispose(); } catch { }
                return;
            }

            if (_Pools.TryGetValue(poolKey, out PooledConnectionCollection? pool))
            {
                if (IsConnectionValid(client) && pool.AvailableCount < _Options.MaxPoolSize)
                {
                    _Logger.LogDebug("Returning connection to pool for {PoolKey}", poolKey);
                    pool.Enqueue(client, DateTime.UtcNow);
                }
                else
                {
                    _Logger.LogDebug("Disposing connection instead of returning to pool for {PoolKey}", poolKey);
                    pool.DecrementTotal();
                    try { client.Disconnect(); } catch { }
                    try { client.Dispose(); } catch { }
                }
            }
        }

        /// <summary>
        /// Removes a connection from the pool (used when connection fails).
        /// </summary>
        /// <param name="poolKey">The pool key.</param>
        internal void RemoveConnection(string poolKey)
        {
            if (_Pools.TryGetValue(poolKey, out PooledConnectionCollection? pool))
            {
                pool.DecrementTotal();
            }
        }

        /// <summary>
        /// Clears all connections in the pool.
        /// </summary>
        public void Clear()
        {
            _Logger.LogDebug("Clearing all connections from pool");

            foreach (PooledConnectionCollection pool in _Pools.Values)
            {
                while (pool.TryDequeue(out NfsClient? client))
                {
                    try { client.Disconnect(); } catch { }
                    try { client.Dispose(); } catch { }
                }
            }

            _Pools.Clear();
        }

        private static string GetPoolKey(IPAddress server, string device, NfsVersion version)
        {
            return $"{server}|{device}|{version}";
        }

        private bool IsConnectionValid(NfsClient client)
        {
            return client.IsConnected && client.IsMounted;
        }

        private void PerformMaintenance(object? state)
        {
            if (_Disposed) return;

            try
            {
                DateTime now = DateTime.UtcNow;
                int cleanedCount = 0;

                foreach (System.Collections.Generic.KeyValuePair<string, PooledConnectionCollection> kvp in _Pools)
                {
                    cleanedCount += kvp.Value.CleanupExpired(_Options.IdleTimeout, now);
                }

                if (cleanedCount > 0)
                {
                    _Logger.LogDebug("Connection pool maintenance cleaned up {Count} idle connections", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "Error during connection pool maintenance");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_Disposed)
            {
                throw new ObjectDisposedException(nameof(NfsConnectionPool));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            _MaintenanceTimer?.Dispose();
            Clear();

            _Logger.LogDebug("NFS Connection Pool disposed");
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_Disposed) return;
            _Disposed = true;

            _MaintenanceTimer?.Dispose();

            foreach (PooledConnectionCollection pool in _Pools.Values)
            {
                while (pool.TryDequeue(out NfsClient? client))
                {
                    try
                    {
                        await client.DisposeAsync().ConfigureAwait(false);
                    }
                    catch { }
                }
            }

            _Pools.Clear();

            _Logger.LogDebug("NFS Connection Pool disposed asynchronously");
        }

        /// <summary>
        /// Internal class to manage a collection of pooled connections.
        /// </summary>
        private sealed class PooledConnectionCollection
        {
            private readonly ConcurrentQueue<(NfsClient Client, DateTime LastUsed)> _Queue = new();
            private int _TotalCount;
            private readonly int _MaxSize;

            /// <summary>
            /// Gets the total count of connections in this collection.
            /// </summary>
            public int TotalCount => _TotalCount;

            /// <summary>
            /// Gets the count of available connections in this collection.
            /// </summary>
            public int AvailableCount => _Queue.Count;

            /// <summary>
            /// Initializes a new instance of the <see cref="PooledConnectionCollection"/> class.
            /// </summary>
            /// <param name="maxSize">The maximum size of the collection.</param>
            public PooledConnectionCollection(int maxSize)
            {
                _MaxSize = maxSize;
            }

            /// <summary>
            /// Enqueues a client to the collection.
            /// </summary>
            /// <param name="client">The client to enqueue.</param>
            /// <param name="lastUsed">The last used time.</param>
            public void Enqueue(NfsClient client, DateTime lastUsed)
            {
                _Queue.Enqueue((client, lastUsed));
            }

            /// <summary>
            /// Tries to dequeue a client from the collection.
            /// </summary>
            /// <param name="client">The dequeued client.</param>
            /// <returns>True if a client was dequeued; otherwise false.</returns>
            public bool TryDequeue(out NfsClient? client)
            {
                if (_Queue.TryDequeue(out (NfsClient Client, DateTime LastUsed) item))
                {
                    client = item.Client;
                    return true;
                }

                client = null;
                return false;
            }

            /// <summary>
            /// Increments the total count.
            /// </summary>
            public void IncrementTotal() => Interlocked.Increment(ref _TotalCount);

            /// <summary>
            /// Decrements the total count.
            /// </summary>
            public void DecrementTotal() => Interlocked.Decrement(ref _TotalCount);

            /// <summary>
            /// Cleans up expired connections.
            /// </summary>
            /// <param name="idleTimeout">The idle timeout.</param>
            /// <param name="now">The current time.</param>
            /// <returns>The number of cleaned up connections.</returns>
            public int CleanupExpired(TimeSpan idleTimeout, DateTime now)
            {
                int cleanedCount = 0;
                ConcurrentQueue<(NfsClient Client, DateTime LastUsed)> requeue = new ConcurrentQueue<(NfsClient Client, DateTime LastUsed)>();

                while (_Queue.TryDequeue(out (NfsClient Client, DateTime LastUsed) item))
                {
                    if (now - item.LastUsed > idleTimeout)
                    {
                        // Connection has been idle too long
                        try { item.Client.Disconnect(); } catch { }
                        try { item.Client.Dispose(); } catch { }
                        Interlocked.Decrement(ref _TotalCount);
                        cleanedCount++;
                    }
                    else
                    {
                        requeue.Enqueue(item);
                    }
                }

                // Re-add valid connections
                while (requeue.TryDequeue(out (NfsClient Client, DateTime LastUsed) item))
                {
                    _Queue.Enqueue(item);
                }

                return cleanedCount;
            }
        }
    }
}
