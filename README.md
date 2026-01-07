# NFS-Client

A comprehensive .NET library for NFS (Network File System) client operations. This library provides a pure C# implementation of the NFS protocol, enabling .NET applications to connect to and interact with NFS servers without requiring any native dependencies.

## About This Repository

This repository is a maintained fork published because the upstream library appeared to be abandoned and required maintenance updates. The library has been updated to target modern .NET frameworks while preserving full backward compatibility.

### Target Frameworks

- .NET Standard 2.1
- .NET 8.0
- .NET 10.0

## Features

- **Multi-Version Protocol Support**: Full client implementation for NFS v2, v3, and v4.1
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **Async/Await**: Complete async API with `CancellationToken` support
- **Connection Pooling**: Built-in connection pool for high-throughput scenarios
- **Health Monitoring**: Automatic connection health checks with configurable heartbeat
- **Dependency Injection**: First-class support for `Microsoft.Extensions.DependencyInjection`
- **File Operations**: Read, write, create, delete, move, and truncate files
- **Directory Operations**: Create, delete, list directories with recursive options
- **Streaming**: Stream-based read/write operations for memory efficiency
- **File Handle Caching**: Optional caching to reduce server round trips
- **Permissions**: Full Unix permission (mode) support
- **Attributes**: Read file/directory metadata including size, timestamps, and type
- **Events**: Data transfer events for progress monitoring

## Installation

Install via NuGet:

```bash
dotnet add package NFS-Client
```

Or via the Package Manager Console:

```powershell
Install-Package NFS-Client
```

## Quick Start

### Basic Usage

```csharp
using NFSLibrary;
using System.Net;

// Create an NFS v3 client
using var client = new NfsClient(NfsVersion.V3);

// Connect to the server
client.Connect(IPAddress.Parse("192.168.1.100"));

// List available exports
var exports = client.GetExportedDevices();
foreach (var export in exports)
{
    Console.WriteLine($"Export: {export}");
}

// Mount an export
client.MountDevice("/srv/nfs");

// List files in the root directory
var files = client.GetItemList(".");
foreach (var file in files)
{
    Console.WriteLine(file);
}

// Create a file and write data
client.CreateFile(".\\myfile.txt");
byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello, NFS!");
client.Write(".\\myfile.txt", 0, data.Length, data);

// Read the file back
byte[] buffer = new byte[1024];
client.Read(".\\myfile.txt", 0, data.Length, ref buffer);
Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer, 0, data.Length));

// Clean up
client.DeleteFile(".\\myfile.txt");
client.UnMountDevice();
client.Disconnect();
```

### Using Connection Options

```csharp
using NFSLibrary;
using System.Net;
using System.Text;

var options = new NfsConnectionOptions
{
    UserId = 1000,              // Unix user ID (default: 0/root)
    GroupId = 1000,             // Unix group ID (default: 0/root)
    CommandTimeoutMs = 30000,   // 30 second timeout (default: 60000)
    CharacterEncoding = Encoding.UTF8,  // File name encoding (default: ASCII)
    UseSecurePort = true,       // Use privileged port <1024 (default: true)
    UseFileHandleCache = true,  // Enable file handle caching (default: false)
    NfsPort = 2049,             // Explicit NFS port (0 = use portmapper)
    MountPort = 0               // Mount protocol port (0 = use portmapper)
};

using var client = new NfsClient(NfsVersion.V3);
client.Connect(IPAddress.Parse("192.168.1.100"), options);
```

### Async Operations

The library provides full async support with cancellation tokens:

```csharp
using NFSLibrary;
using System.Net;

using var client = new NfsClient(NfsVersion.V3);
var cts = new CancellationTokenSource();

// Async connection and mount
await client.ConnectAsync(IPAddress.Parse("192.168.1.100"), cancellationToken: cts.Token);
await client.MountDeviceAsync("/srv/nfs", cts.Token);

// Async file operations
await client.CreateFileAsync(".\\newfile.txt", cancellationToken: cts.Token);

// Read file to stream
using var outputStream = new MemoryStream();
await client.ReadAsync(".\\file.txt", outputStream, cts.Token);

// Write from stream
using var inputStream = new MemoryStream(data);
await client.WriteAsync(".\\newfile.txt", inputStream, cts.Token);

// Read/write with Memory<byte> for zero-allocation scenarios
var readBuffer = new byte[4096];
int bytesRead = await client.ReadAsync(".\\file.txt", offset: 0, readBuffer.AsMemory(), cts.Token);

var writeData = new byte[] { 1, 2, 3, 4 };
int bytesWritten = await client.WriteAsync(".\\file.txt", offset: 0, writeData.AsMemory(), cts.Token);

// Async directory operations
var items = await client.GetItemListAsync(".", cts.Token);

// IAsyncEnumerable for efficient large directory listing
await foreach (var item in client.GetItemsAsync(".", cts.Token))
{
    Console.WriteLine(item);
}

// Async file management
await client.MoveAsync(".\\oldname.txt", ".\\newname.txt", cts.Token);
await client.DeleteFileAsync(".\\file.txt", cts.Token);
await client.DeleteDirectoryAsync(".\\folder", recursive: true, cts.Token);

// Check existence
bool exists = await client.FileExistsAsync(".\\file.txt", cts.Token);
bool isDir = await client.IsDirectoryAsync(".\\folder", cts.Token);

// Get attributes
var attrs = await client.GetItemAttributesAsync(".\\file.txt", cancellationToken: cts.Token);

// Cleanup
await client.UnMountDeviceAsync(cts.Token);
await client.DisconnectAsync(cts.Token);
```

### Stream-Based File Operations

```csharp
// Reading to a stream
using var memoryStream = new MemoryStream();
Stream outputStream = memoryStream;
client.Read(".\\largefile.bin", ref outputStream);

// Reading with cancellation
client.Read(".\\largefile.bin", ref outputStream, cancellationToken);

// Writing from a stream
using var fileStream = File.OpenRead("localfile.txt");
client.Write(".\\remotefile.txt", fileStream);

// Writing with offset (append mode)
client.Write(".\\remotefile.txt", offset: 1024, fileStream);

// Writing from buffer
byte[] data = GetData();
client.Write(".\\remotefile.txt", offset: 0, count: data.Length, buffer: data);

// Writing with total length output
uint totalWritten;
client.Write(".\\remotefile.txt", offset: 0, count: (uint)data.Length, buffer: data, out totalWritten);
```

### Working with File Attributes

```csharp
using NFSLibrary.Protocols.Commons;

// Get file/directory attributes
NFSAttributes attrs = client.GetItemAttributes(".\\myfile.txt");

Console.WriteLine($"Type: {attrs.NFSType}");           // NFREG, NFDIR, NFLNK, etc.
Console.WriteLine($"Size: {attrs.Size} bytes");
Console.WriteLine($"Created: {attrs.CreateDateTime}");
Console.WriteLine($"Modified: {attrs.ModifiedDateTime}");
Console.WriteLine($"Accessed: {attrs.LastAccessedDateTime}");
Console.WriteLine($"Permissions: {attrs.Mode}");
Console.WriteLine($"Handle: {BitConverter.ToString(attrs.Handle)}");

// Optional: don't throw if not found
var attrs2 = client.GetItemAttributes(".\\maynotexist.txt", throwExceptionIfNotFound: false);
if (attrs2 == null)
{
    Console.WriteLine("File does not exist");
}

// Check if path is a directory
bool isDir = client.IsDirectory(".\\somepath");

// Check if file/directory exists
bool exists = client.FileExists(".\\myfile.txt");
```

### Setting Permissions

```csharp
using NFSLibrary.Protocols.Commons;

// NFSPermission takes user, group, other permissions (0-7 each)
// 7 = rwx, 6 = rw-, 5 = r-x, 4 = r--, 0 = ---

// Create file with 755 permissions (rwxr-xr-x)
var execPermission = new NFSPermission(7, 5, 5);
client.CreateFile(".\\script.sh", execPermission);

// Create directory with 775 permissions (rwxrwxr-x)
var dirPermission = new NFSPermission(7, 7, 5);
client.CreateDirectory(".\\shared", dirPermission);

// Set default permissions for all new files/directories
client.Mode = new NFSPermission(6, 4, 4);  // 644 (rw-r--r--)

// Access permission components
Console.WriteLine($"User: {execPermission.UserAccess}");   // 7
Console.WriteLine($"Group: {execPermission.GroupAccess}"); // 5
Console.WriteLine($"Other: {execPermission.OtherAccess}"); // 5
```

## NFS Version Differences

### NFSv2

The original NFS protocol. Simple but limited.

```csharp
var client = new NfsClient(NfsVersion.V2);
```

**Limitations:**
- 32-bit file sizes (max 4GB files)
- Synchronous operations only
- Limited error information

### NFSv3

The most widely supported version. Recommended for most use cases.

```csharp
var client = new NfsClient(NfsVersion.V3);
```

**Features:**
- 64-bit file sizes (large file support)
- Improved caching semantics
- Better error handling
- Weak cache consistency

### NFSv4.1

Modern NFS with integrated security and stateful operations.

```csharp
var client = new NfsClient(NfsVersion.V4);

// NFSv4 uses a single port and doesn't need portmapper
var options = new NfsConnectionOptions { NfsPort = 2049 };
client.Connect(IPAddress.Parse("192.168.1.100"), options);

// NFSv4 mounts the root, then navigates to exports
client.MountDevice("/");
```

**Features:**
- Single TCP port (2049) - no portmapper needed
- Integrated security (RPCSEC_GSS)
- Stateful protocol with sessions
- Compound operations for efficiency
- Delegation for improved caching

**NFSv4 Grace Period:**
After server startup, NFSv4 enters a "grace period" (typically 90 seconds) during which stateful operations may fail with `NFS4ERR_GRACE`. The library handles this automatically during connection.

## Connection Pooling

For applications requiring high throughput or concurrent connections:

```csharp
using NFSLibrary;
using System.Net;

// Configure pool options
var poolOptions = new NfsConnectionPoolOptions
{
    MaxPoolSize = 10,                         // Max connections per server/export
    IdleTimeout = TimeSpan.FromMinutes(5),    // Remove idle connections
    EnableMaintenance = true,                 // Background maintenance
    MaintenanceInterval = TimeSpan.FromMinutes(1)
};

// Create the pool (typically as a singleton)
using var pool = new NfsConnectionPool(poolOptions);

// Acquire a connection from the pool
await using var connection = await pool.GetConnectionAsync(
    server: IPAddress.Parse("192.168.1.100"),
    device: "/srv/nfs",
    version: NfsVersion.V3,
    connectionOptions: new NfsConnectionOptions { UserId = 1000 });

// Use the underlying client
var files = connection.Client.GetItemList(".");
foreach (var file in files)
{
    Console.WriteLine(file);
}

// Connection is automatically returned to pool when disposed

// Monitor pool statistics
Console.WriteLine($"Total connections: {pool.TotalConnections}");
Console.WriteLine($"Available (idle): {pool.AvailableConnections}");
```

## Connection Health Monitoring

Monitor connection health with automatic heartbeat:

```csharp
using NFSLibrary;

// Configure health monitoring
var healthOptions = new NfsConnectionHealthOptions
{
    EnableAutoHeartbeat = true,                   // Automatic health checks
    HeartbeatInterval = TimeSpan.FromSeconds(30), // Check every 30s
    UnhealthyThreshold = 3                        // Unhealthy after 3 failures
};

using var client = new NfsClient(NfsVersion.V3);
client.Connect(IPAddress.Parse("192.168.1.100"));
client.MountDevice("/srv/nfs");

// Create health monitor
using var health = new NfsConnectionHealth(client, healthOptions);

// Subscribe to health status changes
health.HealthStatusChanged += (sender, e) =>
{
    Console.WriteLine($"Health: {e.OldStatus} -> {e.NewStatus}");

    if (e.NewStatus == ConnectionHealthStatus.Unhealthy)
    {
        // Handle unhealthy connection (reconnect, alert, etc.)
    }
};

// Manual health check
HealthCheckResult result = health.CheckHealth();
Console.WriteLine($"Healthy: {result.IsHealthy}");
Console.WriteLine($"Latency: {result.Latency.TotalMilliseconds}ms");
Console.WriteLine($"Message: {result.Message}");
if (result.Exception != null)
{
    Console.WriteLine($"Error: {result.Exception.Message}");
}

// Query current status
Console.WriteLine($"Current Status: {health.Status}");           // Healthy, Degraded, Unhealthy, Unknown
Console.WriteLine($"Last Success: {health.LastSuccessfulCheck}");
Console.WriteLine($"Failures: {health.ConsecutiveFailures}");

// Async health check
var asyncResult = await health.CheckHealthAsync(cancellationToken);

// Attempt reconnection if unhealthy
if (health.Status == ConnectionHealthStatus.Unhealthy)
{
    bool reconnected = health.TryReconnect();
}
```

## Dependency Injection

For .NET 8.0+ applications using `Microsoft.Extensions.DependencyInjection`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using NFSLibrary;
using NFSLibrary.DependencyInjection;

var services = new ServiceCollection();

// Register NFS client with configuration
services.AddNfsClient(options =>
{
    options.Version = NfsVersion.V3;
    options.DefaultServer = "192.168.1.100";
    options.DefaultExport = "/srv/nfs";
});

// Register connection pooling
services.AddNfsConnectionPool(options =>
{
    options.MaxPoolSize = 10;
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    options.EnableMaintenance = true;
});

// Register health monitoring
services.AddNfsHealthChecks(options =>
{
    options.EnableAutoHeartbeat = true;
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
    options.UnhealthyThreshold = 3;
});

// Named configurations for multiple servers
services.AddNfsClient("production", options =>
{
    options.Version = NfsVersion.V4;
    options.DefaultServer = "prod-nfs.example.com";
});

services.AddNfsClient("backup", options =>
{
    options.Version = NfsVersion.V3;
    options.DefaultServer = "backup-nfs.example.com";
});

var provider = services.BuildServiceProvider();

// Resolve services
var factory = provider.GetRequiredService<INfsClientFactory>();

// Create client with default configuration
var defaultClient = factory.CreateClient();

// Create client with named configuration
var prodClient = factory.CreateClient("production");
var backupClient = factory.CreateClient("backup");

// Resolve pool
var pool = provider.GetRequiredService<NfsConnectionPool>();
```

## Data Transfer Events

Monitor file transfer progress:

```csharp
// Subscribe to data transfer events
client.DataEvent += (sender, e) =>
{
    Console.WriteLine($"Transferred: {e.BytesTransferred} bytes");

    // Calculate progress if you know total size
    double progress = (double)e.BytesTransferred / totalSize * 100;
    Console.WriteLine($"Progress: {progress:F1}%");
};

// File operations will now trigger the event
client.Read(".\\largefile.bin", ref outputStream);
client.Write(".\\upload.bin", inputStream);
```

## Error Handling

The library throws specific exceptions for different error conditions:

```csharp
using NFSLibrary;
using NFSLibrary.Protocols.Commons.Exceptions;

try
{
    client.Connect(IPAddress.Parse("192.168.1.100"));
    client.MountDevice("/srv/nfs");
    client.Read(".\\file.txt", ref stream);
}
catch (NFSConnectionException ex)
{
    // Connection failed (server unreachable, timeout, etc.)
    Console.WriteLine($"Connection error: {ex.Message}");
}
catch (NFSMountException ex)
{
    // Mount failed (export not found, permission denied, etc.)
    Console.WriteLine($"Mount error: {ex.Message}");
}
catch (NFSGeneralException ex)
{
    // General NFS operation error
    Console.WriteLine($"NFS error: {ex.Message}");
}
catch (IOException ex)
{
    // I/O error during file operations
    Console.WriteLine($"I/O error: {ex.Message}");
}
```

## API Reference

### NfsClient Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsMounted` | `bool` | Whether an export is currently mounted |
| `IsConnected` | `bool` | Whether connected to a server |
| `CurrentDirectory` | `string` | Current working directory on the server |
| `BlockSize` | `int` | Negotiated I/O block size (read-only) |
| `Mode` | `NFSPermission` | Default permissions for new files/directories |

### NfsClient Methods - Connection

| Method | Description |
|--------|-------------|
| `Connect(IPAddress)` | Connect with default options |
| `Connect(IPAddress, NfsConnectionOptions)` | Connect with options |
| `Connect(IPAddress, int, int, int)` | Connect with userId, groupId, timeout |
| `Connect(IPAddress, int, int, int, Encoding, bool, bool, int, int)` | Full connection options |
| `ConnectAsync(...)` | Async variants of above |
| `Disconnect()` | Close the connection |
| `DisconnectAsync()` | Close connection asynchronously |

### NfsClient Methods - Mount/Export

| Method | Description |
|--------|-------------|
| `GetExportedDevices()` | List available NFS exports |
| `GetExportedDevicesAsync()` | Async variant |
| `MountDevice(string)` | Mount an NFS export |
| `MountDeviceAsync(string)` | Async variant |
| `UnMountDevice()` | Unmount current export |
| `UnMountDeviceAsync()` | Async variant |

### NfsClient Methods - File Operations

| Method | Description |
|--------|-------------|
| `CreateFile(string)` | Create empty file |
| `CreateFile(string, NFSPermission)` | Create file with permissions |
| `CreateFileAsync(...)` | Async variants |
| `DeleteFile(string)` | Delete a file |
| `DeleteFileAsync(string)` | Async variant |
| `Read(...)` | Read file (multiple overloads for files, streams, buffers) |
| `ReadAsync(...)` | Async read operations |
| `Write(...)` | Write file (multiple overloads) |
| `WriteAsync(...)` | Async write operations |
| `Move(string, string)` | Move/rename a file |
| `MoveAsync(...)` | Async variant |
| `SetFileSize(string, long)` | Truncate or extend file |
| `SetFileSizeAsync(...)` | Async variant |

### NfsClient Methods - Directory Operations

| Method | Description |
|--------|-------------|
| `CreateDirectory(string)` | Create directory |
| `CreateDirectory(string, NFSPermission)` | Create with permissions |
| `CreateDirectoryAsync(...)` | Async variants |
| `DeleteDirectory(string)` | Delete empty directory |
| `DeleteDirectory(string, bool)` | Delete with optional recursion |
| `DeleteDirectoryAsync(...)` | Async variants |
| `GetItemList(string)` | List directory contents |
| `GetItemList(string, bool)` | List excluding "." and ".." |
| `GetItemListAsync(...)` | Async variant |
| `GetItemsAsync(...)` | `IAsyncEnumerable` for streaming |

### NfsClient Methods - Attributes & Queries

| Method | Description |
|--------|-------------|
| `GetItemAttributes(string)` | Get file/directory attributes |
| `GetItemAttributes(string, bool)` | With optional throw on not found |
| `GetItemAttributesAsync(...)` | Async variant |
| `FileExists(string)` | Check if path exists |
| `FileExistsAsync(...)` | Async variant |
| `IsDirectory(string)` | Check if path is directory |
| `IsDirectoryAsync(...)` | Async variant |

### NfsClient Methods - Path Utilities

| Method | Description |
|--------|-------------|
| `Combine(string, string)` | Combine path components |
| `GetFileName(string)` | Extract filename from path |
| `GetDirectoryName(string)` | Extract directory from path |
| `CompleteIo()` | Flush pending I/O operations |

### NfsClient Events

| Event | Description |
|-------|-------------|
| `DataEvent` | Fired during data transfer with byte count |

## Test Projects

The solution includes comprehensive test projects for different testing scenarios:

### Test.Unit

**Location:** `test/Test.Unit/`

Unit tests for library internals. Run without an NFS server.

**Test Coverage:**
- XDR encoding/decoding (protocol serialization)
- File handle caching logic
- Connection pool behavior
- NFSv4 protocol message construction
- Permission parsing and formatting
- Exception helper utilities
- Result type handling
- Dependency injection configuration
- Mock client for testing consumer code

**Technologies:** xUnit, FluentAssertions, Moq

**Running:**
```bash
dotnet test test/Test.Unit
```

### Test.Integration

**Location:** `test/Test.Integration/`

Integration tests against real NFS servers using Docker containers.

**Test Coverage:**
- End-to-end file operations (create, read, write, delete)
- Directory operations (create, list, delete recursive)
- Large file transfers (64-bit file sizes)
- Partial reads and writes with offsets
- File attributes and permissions
- Move/rename operations
- NFSv3-specific features (FSINFO, weak cache consistency)
- NFSv4-specific features (sessions, grace period handling)
- Error handling and edge cases

**Prerequisites:**
- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- .NET 8.0 SDK or later
- On Linux: `nfsd` kernel module (`sudo modprobe nfsd`)

**Running:**
```bash
# Start NFS servers
docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml up -d

# Wait for healthy status (~30-45 seconds)
docker ps

# Run all integration tests
dotnet test test/Test.Integration

# Run version-specific tests
dotnet test test/Test.Integration --filter "NfsVersion=3"
dotnet test test/Test.Integration --filter "NfsVersion=4"

# Run specific test class
dotnet test test/Test.Integration --filter "FullyQualifiedName~NfsV3IntegrationTests"

# Stop servers
docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml down
```

**Server Configuration:**

| Version | NFS Port | Portmapper | Container |
|---------|----------|------------|-----------|
| NFSv3 | 22049 | 20111 | nfs-integration-v3 |
| NFSv4 | 32049 | N/A | nfs-integration-v4 |

### Test.Integration.Runner

**Location:** `test/Test.Integration.Runner/`

Console application for running integration tests programmatically.

**Features:**
- Automatic Docker container lifecycle management
- Real-time progress with colored output
- Detailed pass/fail reporting per test
- Summary with timing information
- Proper exit codes for CI/CD pipelines

**Running:**
```bash
dotnet run --project test/Test.Integration.Runner
```

### Test.Interactive

**Location:** `test/Test.Interactive/`

Interactive shell for manual NFS server exploration and testing.

**Features:**
- Automatic Docker NFS server startup
- Shell-like command interface
- File and directory navigation
- All file operations (read, write, delete, copy, move)
- Hex dump for binary files
- Directory tree visualization
- File attribute inspection

**Commands:**
```
Navigation:
  ls, dir [path]        - List directory contents
  cd <path>             - Change directory
  pwd                   - Print working directory
  tree [path] [depth]   - Show directory tree

File Operations:
  cat, read <file>      - Display file (text)
  hex <file>            - Display file (hex dump)
  write <file> <text>   - Write text to file
  append <file> <text>  - Append text to file
  touch <file>          - Create empty file
  rm <file>             - Delete file
  mv <src> <dst>        - Move/rename file
  cp <src> <dst>        - Copy file
  truncate <file> <sz>  - Set file size

Directory Operations:
  mkdir <dir>           - Create directory
  rmdir <dir>           - Delete directory

Information:
  stat <path>           - Show attributes
  exists <path>         - Check existence
  isdir <path>          - Check if directory
  size <file>           - Show file size
  exports               - List NFS exports

Other:
  help                  - Show help
  clear                 - Clear screen
  quit                  - Exit
```

**Running:**
```bash
dotnet run --project test/Test.Interactive
```

### Test.Client

**Location:** `src/Test.Client/`

Simple console client for testing against any NFS server.

**Features:**
- Configurable server address and share
- NFS version selection (V2, V3, V4)
- Basic operations: list shares, enumerate files, read, write, delete
- Directory tree walking

**Running:**
```bash
dotnet run --project src/Test.Client
```

### TestGrace

**Location:** `test/TestGrace/`

Minimal test for NFSv4 grace period behavior debugging.

**Purpose:**
- Verify grace period handling after server startup
- Debug NFSv4 connection issues
- Test stateful operation timing

**Running:**
```bash
# Start NFSv4 server
docker-compose -f test/Test.Integration/Infrastructure/nfsv4/docker-compose.yml up -d

# Run grace period test
dotnet run --project test/TestGrace
```

## Building from Source

```bash
# Clone the repository
git clone https://github.com/SonnyX/NFS-Client.git
cd NFS-Client

# Build the solution
dotnet build src/NFS-Client.sln

# Run unit tests
dotnet test test/Test.Unit

# Run integration tests (requires Docker)
docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml up -d
sleep 45  # Wait for servers to be healthy
dotnet test test/Test.Integration
docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml down
```

## Troubleshooting

### Connection Issues

**"Permission denied" on mount:**
- Ensure the NFS export allows your client IP
- Check if server requires `no_root_squash` for root access
- Verify UID/GID in connection options match server expectations

**Timeout errors:**
- Increase `CommandTimeoutMs` in connection options
- Check network connectivity to NFS server
- Verify firewall allows NFS ports (2049, 111 for portmapper)

**"Program not registered" error:**
- Ensure portmapper (rpcbind) is running on server
- Try specifying `NfsPort` and `MountPort` explicitly

### NFSv4 Issues

**"NFS4ERR_GRACE" errors:**
- Wait 2-3 minutes after server startup for grace period to complete
- The library's integration test infrastructure handles this automatically

**Session errors:**
- NFSv4.1 requires session establishment; ensure server supports 4.1
- Some operations require prior state (OPEN before READ/WRITE)

### Docker Test Issues

**Containers won't start:**
```bash
docker version              # Verify Docker is running
docker ps                   # Check for port conflicts
docker-compose logs         # View container logs
```

**"Docker is not available":**
- Start Docker Desktop/Engine
- On Windows: Ensure Linux containers mode
- On Linux: Add user to `docker` group

## License

This library is licensed under the **GNU Lesser General Public License v3.0 (LGPL-3.0)**.

Per the LGPL-3.0 license terms:

1. **Attribution**: Original copyrights and license notices must be preserved
2. **Source Code**: Modifications to this library must be released under LGPL-3.0
3. **Notice**: Applications using this library must acknowledge its use
4. **Combined Works**: Applications may use different licenses; the library portion remains LGPL

See [LICENSE.txt](LICENSE.txt) for the complete license text.

## Attribution and Copyright

This library incorporates work from multiple sources:

### NekoDrive / NFSLibrary
- **Original Project**: NekoDrive
- **Source**: https://github.com/nekoni/nekodrive (mirror of archived Google Code project)

### NFS-Client Fork
- **Author**: Randy von der Weide (SonnyX)
- **Repository**: https://github.com/SonnyX/NFS-Client

### Remote Tea ONC/RPC for .NET

```
Copyright (c) 1999, 2000
Lehrstuhl fuer Prozessleittechnik (PLT), RWTH Aachen
D-52064 Aachen, Germany.
All rights reserved.

Authors: Harald Albrecht, Jay Walters
License: GNU Library General Public License (LGPL)
```

## Contributing

Contributions are welcome! Please ensure:

1. All original copyright notices are preserved
2. Modifications are released under LGPL-3.0
3. Code follows existing style conventions
4. Tests are included for new functionality

## See Also

- [Integration Testing Guide](INTEGRATION_TEST.md) - Detailed testing documentation
- [RFC 1813 - NFS Version 3](https://tools.ietf.org/html/rfc1813)
- [RFC 7530 - NFS Version 4](https://tools.ietf.org/html/rfc7530)
- [RFC 8881 - NFS Version 4.1](https://tools.ietf.org/html/rfc8881)
