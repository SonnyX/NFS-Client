namespace NFSLibrary
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a pooled NFS connection that returns to the pool when disposed.
    /// </summary>
    public sealed class PooledNfsConnection : IDisposable, IAsyncDisposable
    {
        private readonly NfsConnectionPool _Pool;
        private readonly string _PoolKey;
        private NfsClient? _Client;
        private bool _Disposed;

        /// <summary>
        /// Gets the underlying NFS client.
        /// </summary>
        public NfsClient Client
        {
            get
            {
                if (_Disposed || _Client == null)
                {
                    throw new ObjectDisposedException(nameof(PooledNfsConnection));
                }
                return _Client;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledNfsConnection"/> class.
        /// </summary>
        /// <param name="client">The NFS client.</param>
        /// <param name="pool">The connection pool.</param>
        /// <param name="poolKey">The pool key.</param>
        internal PooledNfsConnection(NfsClient client, NfsConnectionPool pool, string poolKey)
        {
            _Client = client;
            _Pool = pool;
            _PoolKey = poolKey;
        }

        /// <summary>
        /// Marks the connection as faulted, preventing it from being returned to the pool.
        /// Call this when an error occurs that may have corrupted the connection state.
        /// </summary>
        public void MarkFaulted()
        {
            if (_Client != null && !_Disposed)
            {
                _Pool.RemoveConnection(_PoolKey);
                try { _Client.Disconnect(); } catch { }
                try { _Client.Dispose(); } catch { }
                _Client = null;
                _Disposed = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            if (_Client != null)
            {
                _Pool.ReturnConnection(_Client, _PoolKey);
                _Client = null;
            }
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
