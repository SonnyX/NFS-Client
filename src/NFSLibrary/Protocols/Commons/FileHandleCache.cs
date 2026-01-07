namespace NFSLibrary.Protocols.Commons
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// A thread-safe cache for NFS file handles and attributes with automatic expiration.
    /// </summary>
    public class FileHandleCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _Cache = new();
        private readonly TimeSpan _DefaultExpiration;
        private readonly Timer? _CleanupTimer;
        private readonly TimeSpan _CleanupInterval;
        private bool _Disposed;

        /// <summary>
        /// Gets the number of items currently in the cache.
        /// </summary>
        public int Count => _Cache.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHandleCache"/> class.
        /// </summary>
        /// <param name="defaultExpiration">The default expiration time for cache entries. Default is 30 seconds.</param>
        /// <param name="enableAutoCleanup">If true, enables automatic cleanup of expired entries. Default is true.</param>
        /// <param name="cleanupInterval">The interval between automatic cleanup runs. Default is 60 seconds.</param>
        public FileHandleCache(
            TimeSpan? defaultExpiration = null,
            bool enableAutoCleanup = true,
            TimeSpan? cleanupInterval = null)
        {
            _DefaultExpiration = defaultExpiration ?? TimeSpan.FromSeconds(30);
            _CleanupInterval = cleanupInterval ?? TimeSpan.FromSeconds(60);

            if (enableAutoCleanup)
            {
                _CleanupTimer = new Timer(
                    _ => CleanupExpiredEntries(),
                    null,
                    _CleanupInterval,
                    _CleanupInterval);
            }
        }

        /// <summary>
        /// Tries to get a cached entry for the specified path.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="handle">The cached file handle if found.</param>
        /// <param name="attributes">The cached file attributes if found.</param>
        /// <returns>True if a valid (non-expired) entry was found; otherwise false.</returns>
        public bool TryGet(string path, out byte[]? handle, out NFSAttributes? attributes)
        {
            if (_Cache.TryGetValue(path, out CacheEntry entry) && !entry.IsExpired)
            {
                handle = entry.Handle;
                attributes = entry.Attributes;
                return true;
            }

            handle = null;
            attributes = null;
            return false;
        }

        /// <summary>
        /// Gets a cached entry for the specified path.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <returns>The cached NFSAttributes if found and not expired; otherwise null.</returns>
        public NFSAttributes? Get(string path)
        {
            if (_Cache.TryGetValue(path, out CacheEntry entry) && !entry.IsExpired)
            {
                return entry.Attributes;
            }

            return null;
        }

        /// <summary>
        /// Sets or updates a cache entry for the specified path.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="attributes">The file attributes to cache.</param>
        /// <param name="expiration">Optional custom expiration time for this entry.</param>
        public void Set(string path, NFSAttributes attributes, TimeSpan? expiration = null)
        {
            DateTime expirationTime = DateTime.UtcNow + (expiration ?? _DefaultExpiration);
            CacheEntry entry = new CacheEntry(attributes.Handle, attributes, expirationTime);
            _Cache[path] = entry;
        }

        /// <summary>
        /// Sets or updates a cache entry for the specified path with explicit handle.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="handle">The file handle to cache.</param>
        /// <param name="attributes">The file attributes to cache.</param>
        /// <param name="expiration">Optional custom expiration time for this entry.</param>
        public void Set(string path, byte[] handle, NFSAttributes attributes, TimeSpan? expiration = null)
        {
            DateTime expirationTime = DateTime.UtcNow + (expiration ?? _DefaultExpiration);
            CacheEntry entry = new CacheEntry(handle, attributes, expirationTime);
            _Cache[path] = entry;
        }

        /// <summary>
        /// Invalidates (removes) a specific cache entry.
        /// </summary>
        /// <param name="path">The file or directory path to invalidate.</param>
        /// <returns>True if the entry was found and removed; otherwise false.</returns>
        public bool Invalidate(string path)
        {
            return _Cache.TryRemove(path, out _);
        }

        /// <summary>
        /// Invalidates all cache entries whose paths start with the specified prefix.
        /// </summary>
        /// <param name="pathPrefix">The path prefix to match.</param>
        /// <returns>The number of entries that were invalidated.</returns>
        public int InvalidatePrefix(string pathPrefix)
        {
            int count = 0;
            foreach (string key in _Cache.Keys.Where(k => k.StartsWith(pathPrefix, StringComparison.Ordinal)))
            {
                if (_Cache.TryRemove(key, out _))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Invalidates all cache entries that match a path component.
        /// Useful when a directory is renamed or deleted.
        /// </summary>
        /// <param name="pathComponent">The path component to match anywhere in the path.</param>
        /// <returns>The number of entries that were invalidated.</returns>
        public int InvalidateContaining(string pathComponent)
        {
            int count = 0;
            foreach (string key in _Cache.Keys.Where(k => k.Contains(pathComponent)))
            {
                if (_Cache.TryRemove(key, out _))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        public void Clear()
        {
            _Cache.Clear();
        }

        /// <summary>
        /// Manually removes all expired entries from the cache.
        /// </summary>
        /// <returns>The number of expired entries that were removed.</returns>
        public int CleanupExpiredEntries()
        {
            int count = 0;
            DateTime now = DateTime.UtcNow;

            foreach (KeyValuePair<string, CacheEntry> kvp in _Cache)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    if (_Cache.TryRemove(kvp.Key, out _))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if a path exists in the cache and is not expired.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <returns>True if a valid (non-expired) entry exists; otherwise false.</returns>
        public bool Contains(string path)
        {
            return _Cache.TryGetValue(path, out CacheEntry entry) && !entry.IsExpired;
        }

        /// <summary>
        /// Gets or adds a cache entry using a factory function.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="factory">A function that produces the attributes if not cached.</param>
        /// <param name="expiration">Optional custom expiration time for new entries.</param>
        /// <returns>The cached or newly created NFSAttributes.</returns>
        public NFSAttributes GetOrAdd(string path, Func<string, NFSAttributes> factory, TimeSpan? expiration = null)
        {
            if (_Cache.TryGetValue(path, out CacheEntry existingEntry) && !existingEntry.IsExpired)
            {
                return existingEntry.Attributes;
            }

            NFSAttributes attributes = factory(path);
            Set(path, attributes, expiration);
            return attributes;
        }

        /// <summary>
        /// Updates the expiration time for an existing cache entry.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="expiration">The new expiration time from now.</param>
        /// <returns>True if the entry was found and updated; otherwise false.</returns>
        public bool Touch(string path, TimeSpan? expiration = null)
        {
            if (_Cache.TryGetValue(path, out CacheEntry entry) && !entry.IsExpired)
            {
                DateTime expirationTime = DateTime.UtcNow + (expiration ?? _DefaultExpiration);
                CacheEntry newEntry = new CacheEntry(entry.Handle, entry.Attributes, expirationTime);
                _Cache[path] = newEntry;
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_Disposed)
                return;

            _CleanupTimer?.Dispose();
            _Cache.Clear();
            _Disposed = true;
        }

        /// <summary>
        /// Represents a single cache entry with handle, attributes, and expiration time.
        /// </summary>
        private readonly struct CacheEntry
        {
            /// <summary>
            /// Gets the file handle.
            /// </summary>
            public byte[] Handle { get; }

            /// <summary>
            /// Gets the file attributes.
            /// </summary>
            public NFSAttributes Attributes { get; }

            /// <summary>
            /// Gets the expiration time.
            /// </summary>
            public DateTime ExpiresAt { get; }

            /// <summary>
            /// Gets a value indicating whether this entry is expired.
            /// </summary>
            public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

            /// <summary>
            /// Initializes a new instance of the <see cref="CacheEntry"/> struct.
            /// </summary>
            /// <param name="handle">The file handle.</param>
            /// <param name="attributes">The file attributes.</param>
            /// <param name="expiresAt">The expiration time.</param>
            public CacheEntry(byte[] handle, NFSAttributes attributes, DateTime expiresAt)
            {
                Handle = handle;
                Attributes = attributes;
                ExpiresAt = expiresAt;
            }
        }
    }
}
