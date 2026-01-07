# Integration Testing Guide

This guide covers running integration tests against real NFS servers using Docker.

## Prerequisites

- **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux)
- **.NET 8.0 SDK** or later
- On Linux: `nfsd` kernel module (`sudo modprobe nfsd`)

## Quick Start

```bash
# 1. Start all NFS servers (from repository root)
docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml up -d

# 2. Wait for servers to be healthy (~30 seconds)
docker ps  # All containers should show "healthy"

# 3. Run integration tests
dotnet test test/Test.Integration

# 4. Stop servers when done
docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml down
```

## Server Configuration

| Version | NFS Port | Portmapper | Container Name |
|---------|----------|------------|----------------|
| NFSv3   | 22049    | 20111      | nfs-integration-v3 |
| NFSv4   | 32049    | N/A        | nfs-integration-v4 |

**Note**: NFSv2 is not supported by the `erichough/nfs-server` Docker image.

Each server exposes one export:
- `/export` - Read-write export for all tests

## Running Specific Tests

```bash
# Run all integration tests
dotnet test test/Test.Integration

# Run tests for a specific NFS version
dotnet test test/Test.Integration --filter "NfsVersion=3"
dotnet test test/Test.Integration --filter "NfsVersion=4"

# Run only integration tests (excludes unit tests if in same solution)
dotnet test test/Test.Integration --filter "Category=Integration"

# Run a specific test class
dotnet test test/Test.Integration --filter "FullyQualifiedName~NfsV3IntegrationTests"
```

## Starting Individual Servers

```bash
# NFSv3 only
docker-compose -f test/Test.Integration/Infrastructure/nfsv3/docker-compose.yml up -d

# NFSv4 only
docker-compose -f test/Test.Integration/Infrastructure/nfsv4/docker-compose.yml up -d
```

## Test Fixtures

Tests use xUnit collection fixtures for container lifecycle management:

- **Auto-start**: Fixtures automatically start Docker containers when tests begin
- **Shared state**: Containers are shared across all tests in a collection
- **Auto-cleanup**: Containers are stopped when all tests complete

If Docker is unavailable, tests fail with a clear error message explaining the requirement.

## Test Data

Tests create their own test data during execution and clean up afterward. The `/export` volume starts empty and tests are responsible for creating any files they need.

## NFSv4 Grace Period

NFSv4 servers implement a "grace period" after startup to allow clients to reclaim locks from a previous server session. During this period, stateful operations like file creation and deletion will fail with `NFS4ERR_GRACE`.

### How It Works

1. When an NFSv4 server starts, it enters a grace period (typically 90 seconds by default)
2. The containerized server may trigger multiple overlapping grace periods for different network namespaces
3. While the docker-compose configuration sets `--grace-time 10`, actual wait times are longer

### How Tests Handle It

The integration test runner handles the grace period automatically:

1. **Initial Wait**: After detecting the NFSv4 server is running, waits ~100 seconds
2. **Active Probing**: Attempts file create/delete operations to verify the grace period has ended
3. **Confirmation**: Requires 3 consecutive successful operations before proceeding
4. **Stability Delay**: Adds a small buffer after confirmation

### Manual Testing

If running tests manually and you encounter `NFS4ERR_GRACE` errors:

```bash
# Wait for the grace period to end (can take 2-3 minutes)
# Then retry your operation

# You can check server status with:
docker exec nfs-integration-v4 cat /proc/fs/nfsd/threads
```

### Why Multiple Grace Periods?

The Docker container runs in a separate network namespace, which can cause the NFS server to detect multiple "network restarts" and trigger additional grace periods. This is a known behavior when running NFS in containers.

## Troubleshooting

### Containers won't start

```bash
# Check if Docker is running
docker version

# Check for port conflicts
netstat -an | findstr "2049"   # Windows
netstat -an | grep 2049        # Linux/Mac

# View container logs
docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml logs
```

### Permission errors

The exports are configured with `no_root_squash` and `insecure` for testing. If you still get permission errors:

```bash
# Verify exports are available
docker exec nfs-integration-v3 showmount -e localhost
```

### Tests fail with "Docker is not available"

1. Ensure Docker Desktop/Engine is running
2. On Windows: Check Docker Desktop is in Linux containers mode
3. On Linux: Ensure your user is in the `docker` group

### Containers show "unhealthy"

```bash
# Check container health
docker inspect nfs-integration-v3 --format='{{.State.Health.Status}}'

# View health check logs
docker inspect nfs-integration-v3 --format='{{json .State.Health}}'
```

## Architecture

```
test/Test.Integration/
├── Infrastructure/           # Docker configuration
│   ├── docker-compose.yml    # Combined (both servers)
│   ├── nfsv3/                # NFSv3 server config
│   └── nfsv4/                # NFSv4 server config
├── Fixtures/                 # Test fixtures (container lifecycle)
├── Helpers/                  # DockerHelper, TestDataGenerator
└── Tests/                    # Integration test classes
```

## CI/CD Considerations

For CI pipelines, ensure:

1. Docker is available in the build environment
2. The `nfsd` kernel module is loaded (Linux)
3. Tests have adequate timeout (containers take ~30s to start)
4. Consider running versions in parallel with separate jobs

Example GitHub Actions setup:

```yaml
- name: Start NFS servers
  run: docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml up -d

- name: Wait for healthy
  run: sleep 45

- name: Run integration tests
  run: dotnet test test/Test.Integration
```
