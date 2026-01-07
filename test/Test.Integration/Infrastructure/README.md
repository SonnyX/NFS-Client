# NFS Integration Test Infrastructure

This directory contains Docker Compose configurations for running NFS servers for integration testing.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose v2+
- On Linux: `nfsd` kernel module must be loaded (`sudo modprobe nfsd`)

## Quick Start

### Start All Servers

```bash
# From the Infrastructure directory
docker-compose up -d

# Or from the repository root
docker-compose -f test/Test.Integration/Infrastructure/docker-compose.yml up -d
```

### Start Individual Servers

```bash
# NFSv2 only
docker-compose -f test/Test.Integration/Infrastructure/nfsv2/docker-compose.yml up -d

# NFSv3 only
docker-compose -f test/Test.Integration/Infrastructure/nfsv3/docker-compose.yml up -d

# NFSv4 only
docker-compose -f test/Test.Integration/Infrastructure/nfsv4/docker-compose.yml up -d
```

### Stop Servers

```bash
docker-compose down
```

## Port Mappings

| Version | NFS Port | Portmapper | Callbacks |
|---------|----------|------------|-----------|
| NFSv2   | 12049    | 10111      | 12765, 12767 |
| NFSv3   | 22049    | 20111      | 22765, 22767 |
| NFSv4   | 32049    | N/A        | N/A |

## Exports

Each server exposes two NFS exports:

| Export | Path | Access | Purpose |
|--------|------|--------|---------|
| Read-Write | `/export/rw` | rw | Write tests (starts empty) |
| Read-Only | `/export/ro` | ro | Read tests (pre-seeded data) |

## Pre-seeded Test Data

The `/export/ro` mount contains:

```
/export/ro/
├── testfile.txt      # Text file with known content
├── subdir/
│   ├── nested.txt    # Nested file for path tests
│   └── empty/        # Empty subdirectory
```

## Connecting from Tests

### NFSv2
```csharp
var client = new NfsClient(NfsVersion.V2);
client.Connect(IPAddress.Loopback, 0, 0, 60000);
// Use portmapper on port 10111, NFS on 12049
```

### NFSv3
```csharp
var client = new NfsClient(NfsVersion.V3);
client.Connect(IPAddress.Loopback, 0, 0, 60000);
// Use portmapper on port 20111, NFS on 22049
```

### NFSv4
```csharp
var client = new NfsClient(NfsVersion.V4);
client.Connect(IPAddress.Loopback, 0, 0, 60000);
// Direct connection to port 32049
```

## Troubleshooting

### Container won't start

1. Ensure Docker is running
2. Check if ports are available: `netstat -an | findstr 2049`
3. View container logs: `docker-compose logs -f`

### Permission denied

The exports are configured with `no_root_squash` and `insecure` to allow testing from unprivileged ports. If you still get permission errors:

1. Check container health: `docker ps`
2. Verify exports: `docker exec nfs-integration-v3 showmount -e localhost`

### NFS module not loaded (Linux)

```bash
sudo modprobe nfsd
# To persist across reboots:
echo "nfsd" | sudo tee -a /etc/modules
```

## Container Details

All containers use the `erichough/nfs-server` image which provides:
- Full NFSv2, NFSv3, and NFSv4 support
- Built-in portmapper
- Health checks
- Configurable exports via environment variables

For more details, see: https://hub.docker.com/r/erichough/nfs-server
