using NFSLibrary;
using NFSLibrary.Protocols.Commons;
using System.Net;

namespace Test.Unit.Mocks;

/// <summary>
/// A mock implementation of INfsClient for testing purposes.
/// Simulates an in-memory file system without requiring a real NFS server.
/// </summary>
public class MockNfsClient : INfsClient
{
    private readonly Dictionary<string, MockFileEntry> _fileSystem = new();
    private bool _isConnected;
    private bool _isMounted;
    private string _mountedDevice = string.Empty;
    private bool _disposed;

    public bool IsMounted => _isMounted;
    public bool IsConnected => _isConnected;
    public NFSPermission Mode { get; set; } = new NFSPermission(7, 7, 7);
    public string CurrentDirectory => ".";
    public int BlockSize => 8192;

    public event NfsClient.NfsDataEventHandler? DataEvent;

    /// <summary>
    /// Gets the simulated file system for test verification.
    /// </summary>
    public IReadOnlyDictionary<string, MockFileEntry> FileSystem => _fileSystem;

    public void Connect(IPAddress address)
    {
        Connect(address, null);
    }

    public void Connect(IPAddress address, NfsConnectionOptions? options)
    {
        _isConnected = true;
    }

    public void Connect(IPAddress address, int userId, int groupId, int commandTimeout)
    {
        _isConnected = true;
    }

    public void Connect(IPAddress address, int userId, int groupId, int commandTimeout,
        System.Text.Encoding characterEncoding, bool useSecurePort, bool useCache, int nfsPort = 0, int mountPort = 0)
    {
        _isConnected = true;
    }

    public void Disconnect()
    {
        _isConnected = false;
        _isMounted = false;
        _mountedDevice = string.Empty;
    }

    public List<string> GetExportedDevices()
    {
        ThrowIfNotConnected();
        return new List<string> { "/export/test", "/export/data", "/export/share" };
    }

    public void MountDevice(string deviceName)
    {
        ThrowIfNotConnected();
        _mountedDevice = deviceName;
        _isMounted = true;

        // Initialize root directory
        EnsureDirectory(".");
    }

    public void UnMountDevice()
    {
        ThrowIfNotMounted();
        _isMounted = false;
        _mountedDevice = string.Empty;
    }

    public List<string> GetItemList(string directoryFullName)
    {
        return GetItemList(directoryFullName, false);
    }

    public List<string> GetItemList(string directoryFullName, bool excludeNavigationDots)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(directoryFullName);
        var items = new List<string>();

        if (!excludeNavigationDots)
        {
            items.Add(".");
            items.Add("..");
        }

        foreach (var entry in _fileSystem)
        {
            var parentDir = GetParentPath(entry.Key);
            // Handle root directory case
            var normalizedParent = string.IsNullOrEmpty(normalizedPath) ? "" : normalizedPath;
            var entryParent = string.IsNullOrEmpty(parentDir) || parentDir == "." ? "" : parentDir;

            if (entryParent == normalizedParent && entry.Key != normalizedPath)
            {
                items.Add(GetFileName(entry.Key));
            }
        }

        return items;
    }

    public NFSAttributes GetItemAttributes(string itemFullName, bool throwExceptionIfNotFound = true)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(itemFullName);

        if (_fileSystem.TryGetValue(normalizedPath, out var entry))
        {
            return entry.ToNFSAttributes();
        }

        if (throwExceptionIfNotFound)
        {
            throw new FileNotFoundException($"Item not found: {itemFullName}");
        }

        return null!;
    }

    public void CreateDirectory(string directoryFullName)
    {
        CreateDirectory(directoryFullName, Mode);
    }

    public void CreateDirectory(string directoryFullName, NFSPermission mode)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(directoryFullName);
        EnsureParentExists(normalizedPath);

        _fileSystem[normalizedPath] = new MockFileEntry
        {
            Path = normalizedPath,
            IsDirectory = true,
            Mode = mode,
            CreateTime = DateTimeOffset.UtcNow,
            ModifiedTime = DateTimeOffset.UtcNow,
            AccessTime = DateTimeOffset.UtcNow
        };
    }

    public void DeleteDirectory(string directoryFullName)
    {
        DeleteDirectory(directoryFullName, false);
    }

    public void DeleteDirectory(string directoryFullName, bool recursive)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(directoryFullName);

        if (!_fileSystem.ContainsKey(normalizedPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryFullName}");
        }

        if (recursive)
        {
            var toRemove = _fileSystem.Keys.Where(k => k.StartsWith(normalizedPath + "\\") || k == normalizedPath).ToList();
            foreach (var key in toRemove)
            {
                _fileSystem.Remove(key);
            }
        }
        else
        {
            _fileSystem.Remove(normalizedPath);
        }
    }

    public void DeleteFile(string fileFullName)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(fileFullName);

        if (!_fileSystem.Remove(normalizedPath))
        {
            throw new FileNotFoundException($"File not found: {fileFullName}");
        }
    }

    public void CreateFile(string fileFullName)
    {
        CreateFile(fileFullName, Mode);
    }

    public void CreateFile(string fileFullName, NFSPermission mode)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(fileFullName);
        EnsureParentExists(normalizedPath);

        _fileSystem[normalizedPath] = new MockFileEntry
        {
            Path = normalizedPath,
            IsDirectory = false,
            Mode = mode,
            Data = Array.Empty<byte>(),
            CreateTime = DateTimeOffset.UtcNow,
            ModifiedTime = DateTimeOffset.UtcNow,
            AccessTime = DateTimeOffset.UtcNow
        };
    }

    public void Read(IEnumerable<string> sourceFileNames, string sourceDirectoryFullName, string destinationDirectoryFullName)
    {
        ThrowIfNotMounted();

        foreach (var fileName in sourceFileNames)
        {
            var sourcePath = Combine(fileName, sourceDirectoryFullName);
            var destPath = Path.Combine(destinationDirectoryFullName, fileName);
            Read(sourcePath, destPath);
        }
    }

    public void Read(string sourceFileFullName, string destinationFileFullName)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(sourceFileFullName);

        if (!_fileSystem.TryGetValue(normalizedPath, out var entry))
        {
            throw new FileNotFoundException($"File not found: {sourceFileFullName}");
        }

        File.WriteAllBytes(destinationFileFullName, entry.Data);
        OnDataEvent(entry.Data.Length);
    }

    public void Read(string sourceFileFullName, ref Stream outputStream)
    {
        Read(sourceFileFullName, ref outputStream, CancellationToken.None);
    }

    public void Read(string sourceFileFullName, ref Stream outputStream, CancellationToken cancellationToken)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(sourceFileFullName);

        if (!_fileSystem.TryGetValue(normalizedPath, out var entry))
        {
            throw new FileNotFoundException($"File not found: {sourceFileFullName}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        outputStream.Write(entry.Data, 0, entry.Data.Length);
        OnDataEvent(entry.Data.Length);
    }

    public long Read(string sourceFileFullName, long offset, long totalLength, ref byte[] buffer)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(sourceFileFullName);

        if (!_fileSystem.TryGetValue(normalizedPath, out var entry))
        {
            throw new FileNotFoundException($"File not found: {sourceFileFullName}");
        }

        var readLength = (int)Math.Min(totalLength, entry.Data.Length - offset);
        buffer = new byte[readLength];
        Array.Copy(entry.Data, offset, buffer, 0, readLength);
        OnDataEvent(readLength);
        return readLength;
    }

    public void Read(string sourceFileFullName, long offset, ref long totalLength, ref byte[] buffer)
    {
        totalLength = Read(sourceFileFullName, offset, totalLength, ref buffer);
    }

    public void Write(string destinationFileFullName, string sourceFileFullName)
    {
        ThrowIfNotMounted();

        var data = File.ReadAllBytes(sourceFileFullName);
        var normalizedPath = NormalizePath(destinationFileFullName);

        EnsureParentExists(normalizedPath);

        _fileSystem[normalizedPath] = new MockFileEntry
        {
            Path = normalizedPath,
            IsDirectory = false,
            Mode = Mode,
            Data = data,
            CreateTime = DateTimeOffset.UtcNow,
            ModifiedTime = DateTimeOffset.UtcNow,
            AccessTime = DateTimeOffset.UtcNow
        };

        OnDataEvent(data.Length);
    }

    public void Write(string destinationFileFullName, Stream inputStream)
    {
        Write(destinationFileFullName, 0, inputStream);
    }

    public void Write(string destinationFileFullName, long inputOffset, Stream inputStream)
    {
        Write(destinationFileFullName, inputOffset, inputStream, CancellationToken.None);
    }

    public void Write(string destinationFileFullName, long inputOffset, Stream inputStream, CancellationToken cancellationToken)
    {
        ThrowIfNotMounted();
        cancellationToken.ThrowIfCancellationRequested();

        using var memStream = new MemoryStream();
        inputStream.CopyTo(memStream);
        var data = memStream.ToArray();

        var normalizedPath = NormalizePath(destinationFileFullName);
        EnsureParentExists(normalizedPath);

        if (_fileSystem.TryGetValue(normalizedPath, out var existing))
        {
            // Append/overwrite at offset
            var newData = new byte[Math.Max(existing.Data.Length, inputOffset + data.Length)];
            Array.Copy(existing.Data, newData, existing.Data.Length);
            Array.Copy(data, 0, newData, inputOffset, data.Length);
            existing.Data = newData;
            existing.ModifiedTime = DateTimeOffset.UtcNow;
        }
        else
        {
            _fileSystem[normalizedPath] = new MockFileEntry
            {
                Path = normalizedPath,
                IsDirectory = false,
                Mode = Mode,
                Data = data,
                CreateTime = DateTimeOffset.UtcNow,
                ModifiedTime = DateTimeOffset.UtcNow,
                AccessTime = DateTimeOffset.UtcNow
            };
        }

        OnDataEvent(data.Length);
    }

    public void Write(string destinationFileFullName, long offset, int count, byte[] buffer)
    {
        uint totalWritten;
        Write(destinationFileFullName, offset, (uint)count, buffer, out totalWritten);
    }

    public void Write(string destinationFileFullName, long offset, uint count, byte[] buffer, out uint totalLength)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(destinationFileFullName);
        EnsureParentExists(normalizedPath);

        var dataToWrite = new byte[count];
        Array.Copy(buffer, 0, dataToWrite, 0, count);

        if (_fileSystem.TryGetValue(normalizedPath, out var existing))
        {
            var newData = new byte[Math.Max(existing.Data.Length, offset + count)];
            Array.Copy(existing.Data, newData, existing.Data.Length);
            Array.Copy(dataToWrite, 0, newData, offset, count);
            existing.Data = newData;
            existing.ModifiedTime = DateTimeOffset.UtcNow;
        }
        else
        {
            var data = new byte[offset + count];
            Array.Copy(dataToWrite, 0, data, offset, count);
            _fileSystem[normalizedPath] = new MockFileEntry
            {
                Path = normalizedPath,
                IsDirectory = false,
                Mode = Mode,
                Data = data,
                CreateTime = DateTimeOffset.UtcNow,
                ModifiedTime = DateTimeOffset.UtcNow,
                AccessTime = DateTimeOffset.UtcNow
            };
        }

        totalLength = count;
        OnDataEvent((int)count);
    }

    public void Move(string sourceFileFullName, string targetFileFullName)
    {
        ThrowIfNotMounted();

        var sourcePath = NormalizePath(sourceFileFullName);
        var targetPath = NormalizePath(targetFileFullName);

        if (!_fileSystem.TryGetValue(sourcePath, out var entry))
        {
            throw new FileNotFoundException($"Source file not found: {sourceFileFullName}");
        }

        _fileSystem.Remove(sourcePath);
        entry.Path = targetPath;
        _fileSystem[targetPath] = entry;
    }

    public bool IsDirectory(string directoryFullName)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(directoryFullName);
        return _fileSystem.TryGetValue(normalizedPath, out var entry) && entry.IsDirectory;
    }

    public void CompleteIo()
    {
        // No-op for mock
    }

    public bool FileExists(string fileFullName)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(fileFullName);
        return _fileSystem.ContainsKey(normalizedPath);
    }

    public string GetFileName(string fileFullName)
    {
        return Path.GetFileName(fileFullName.Replace('/', '\\'));
    }

    public string GetDirectoryName(string fullDirectoryName)
    {
        var result = Path.GetDirectoryName(fullDirectoryName.Replace('/', '\\'));
        return string.IsNullOrEmpty(result) ? "." : result;
    }

    public string Combine(string fileName, string directoryFullName)
    {
        if (directoryFullName == "." || string.IsNullOrEmpty(directoryFullName))
            return fileName;
        return directoryFullName.TrimEnd('\\') + "\\" + fileName;
    }

    public void SetFileSize(string fileFullName, long size)
    {
        ThrowIfNotMounted();

        var normalizedPath = NormalizePath(fileFullName);

        if (!_fileSystem.TryGetValue(normalizedPath, out var entry))
        {
            throw new FileNotFoundException($"File not found: {fileFullName}");
        }

        if (entry.IsDirectory)
        {
            throw new InvalidOperationException("Cannot set file size on a directory");
        }

        var newData = new byte[size];
        Array.Copy(entry.Data, newData, Math.Min(entry.Data.Length, size));
        entry.Data = newData;
        entry.ModifiedTime = DateTimeOffset.UtcNow;
    }

    public void Dispose()
    {
        if (_disposed) return;

        Disconnect();
        _fileSystem.Clear();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }

    #region Async Methods

    public Task ConnectAsync(IPAddress address, NfsConnectionOptions? options = null, CancellationToken cancellationToken = default)
    {
        Connect(address, options);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        Disconnect();
        return Task.CompletedTask;
    }

    public Task<List<string>> GetExportedDevicesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetExportedDevices());

    public Task MountDeviceAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        MountDevice(deviceName);
        return Task.CompletedTask;
    }

    public Task UnMountDeviceAsync(CancellationToken cancellationToken = default)
    {
        UnMountDevice();
        return Task.CompletedTask;
    }

    public Task<List<string>> GetItemListAsync(string directoryFullName, CancellationToken cancellationToken = default)
        => Task.FromResult(GetItemList(directoryFullName));

    public async IAsyncEnumerable<string> GetItemsAsync(
        string directoryFullName, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in GetItemList(directoryFullName, excludeNavigationDots: true))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }

    public Task<NFSAttributes?> GetItemAttributesAsync(string itemFullName, bool throwExceptionIfNotFound = true, CancellationToken cancellationToken = default)
        => Task.FromResult<NFSAttributes?>(GetItemAttributes(itemFullName, throwExceptionIfNotFound));

    public Task CreateDirectoryAsync(string directoryFullName, NFSPermission? mode = null, CancellationToken cancellationToken = default)
    {
        CreateDirectory(directoryFullName, mode ?? Mode);
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string directoryFullName, bool recursive = false, CancellationToken cancellationToken = default)
    {
        DeleteDirectory(directoryFullName, recursive);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string fileFullName, CancellationToken cancellationToken = default)
    {
        DeleteFile(fileFullName);
        return Task.CompletedTask;
    }

    public Task CreateFileAsync(string fileFullName, NFSPermission? mode = null, CancellationToken cancellationToken = default)
    {
        CreateFile(fileFullName, mode ?? Mode);
        return Task.CompletedTask;
    }

    public Task ReadAsync(string sourceFileFullName, Stream outputStream, CancellationToken cancellationToken = default)
    {
        Read(sourceFileFullName, ref outputStream, cancellationToken);
        return Task.CompletedTask;
    }

    public Task<int> ReadAsync(string sourceFileFullName, long offset, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var tempBuffer = new byte[buffer.Length];
        var bytesRead = (int)Read(sourceFileFullName, offset, buffer.Length, ref tempBuffer);
        tempBuffer.AsMemory(0, bytesRead).CopyTo(buffer);
        return Task.FromResult(bytesRead);
    }

    public Task WriteAsync(string destinationFileFullName, Stream inputStream, CancellationToken cancellationToken = default)
    {
        Write(destinationFileFullName, inputStream);
        return Task.CompletedTask;
    }

    public Task<int> WriteAsync(string destinationFileFullName, long offset, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Write(destinationFileFullName, offset, buffer.Length, buffer.ToArray());
        return Task.FromResult(buffer.Length);
    }

    public Task MoveAsync(string sourceFileFullName, string targetFileFullName, CancellationToken cancellationToken = default)
    {
        Move(sourceFileFullName, targetFileFullName);
        return Task.CompletedTask;
    }

    public Task SetFileSizeAsync(string fileFullName, long size, CancellationToken cancellationToken = default)
    {
        SetFileSize(fileFullName, size);
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string fileFullName, CancellationToken cancellationToken = default)
        => Task.FromResult(FileExists(fileFullName));

    public Task<bool> IsDirectoryAsync(string directoryFullName, CancellationToken cancellationToken = default)
        => Task.FromResult(IsDirectory(directoryFullName));

    #endregion

    #region Helper Methods

    private void ThrowIfNotConnected()
    {
        if (!_isConnected)
            throw new InvalidOperationException("Not connected to NFS server");
    }

    private void ThrowIfNotMounted()
    {
        ThrowIfNotConnected();
        if (!_isMounted)
            throw new InvalidOperationException("No device mounted");
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return ".";
        return path.Replace('/', '\\').TrimStart('.').TrimStart('\\');
    }

    private static string GetParentPath(string path)
    {
        var normalized = NormalizePath(path);
        var lastSep = normalized.LastIndexOf('\\');
        return lastSep < 0 ? "." : normalized.Substring(0, lastSep);
    }

    private void EnsureDirectory(string path)
    {
        var normalized = NormalizePath(path);
        if (!_fileSystem.ContainsKey(normalized) && normalized != "")
        {
            _fileSystem[normalized] = new MockFileEntry
            {
                Path = normalized,
                IsDirectory = true,
                Mode = Mode,
                CreateTime = DateTimeOffset.UtcNow,
                ModifiedTime = DateTimeOffset.UtcNow,
                AccessTime = DateTimeOffset.UtcNow
            };
        }
    }

    private void EnsureParentExists(string path)
    {
        var parent = GetParentPath(path);
        if (parent != "." && parent != "")
        {
            EnsureDirectory(parent);
        }
    }

    private void OnDataEvent(int bytes)
    {
        DataEvent?.Invoke(this, new NfsEventArgs(bytes));
    }

    #endregion
}

/// <summary>
/// Represents a file or directory entry in the mock file system.
/// </summary>
public class MockFileEntry
{
    public string Path { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public NFSPermission Mode { get; set; } = new NFSPermission(7, 7, 7);
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset ModifiedTime { get; set; }
    public DateTimeOffset AccessTime { get; set; }

    public NFSAttributes ToNFSAttributes()
    {
        return new NFSAttributes(
            (int)CreateTime.ToUnixTimeSeconds(),
            (int)AccessTime.ToUnixTimeSeconds(),
            (int)ModifiedTime.ToUnixTimeSeconds(),
            IsDirectory ? NFSItemTypes.NFDIR : NFSItemTypes.NFREG,
            Mode,
            Data.Length,
            System.Text.Encoding.UTF8.GetBytes(Path)
        );
    }
}
