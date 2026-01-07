using System.Net;
using NFSLibrary;
using NFSLibrary.Protocols.Commons;
using Test.Integration.Helpers;

namespace Test.Integration.Fixtures;

/// <summary>
/// Base class for NFS server test fixtures.
/// Manages the Docker container lifecycle for NFS integration tests.
/// </summary>
public abstract class NfsServerFixture : IAsyncLifetime
{
    private bool _containerStarted;
    private bool _disposed;

    /// <summary>
    /// Gets the path to the Docker Compose file for this server.
    /// </summary>
    protected abstract string ComposeFilePath { get; }

    /// <summary>
    /// Gets the name of the Docker service.
    /// </summary>
    protected abstract string ServiceName { get; }

    /// <summary>
    /// Gets the container name for health checks.
    /// </summary>
    protected abstract string ContainerName { get; }

    /// <summary>
    /// Gets the NFS server address (always localhost for Docker).
    /// </summary>
    public IPAddress ServerAddress => IPAddress.Loopback;

    /// <summary>
    /// Gets the NFS port for this server version.
    /// </summary>
    public abstract int NfsPort { get; }

    /// <summary>
    /// Gets the portmapper port (if applicable).
    /// </summary>
    public abstract int? PortmapperPort { get; }

    /// <summary>
    /// Gets the mount protocol port (NFSv2/v3 only).
    /// </summary>
    public virtual int? MountPort => null;

    /// <summary>
    /// Gets the NFS version this fixture provides.
    /// </summary>
    public abstract NfsVersion Version { get; }

    /// <summary>
    /// Gets the export path (used for all tests).
    /// </summary>
    public string Export => "/export";

    /// <summary>
    /// Gets whether Docker is available on this system.
    /// </summary>
    public bool IsDockerAvailable { get; private set; }

    /// <summary>
    /// Gets whether the NFS container is running and healthy.
    /// </summary>
    public bool IsServerReady { get; private set; }

    /// <summary>
    /// Gets the timeout for container startup.
    /// </summary>
    protected virtual TimeSpan StartupTimeout => TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets the timeout for server readiness checks.
    /// </summary>
    protected virtual TimeSpan ReadinessTimeout => TimeSpan.FromSeconds(60);

    /// <summary>
    /// Initializes the fixture by starting the Docker container.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Check if Docker is available
        IsDockerAvailable = await DockerHelper.IsDockerAvailableAsync();
        if (!IsDockerAvailable)
        {
            Console.WriteLine("WARNING: Docker is not available. Integration tests will be skipped.");
            return;
        }

        try
        {
            // Check if container is already running
            var isRunning = await DockerHelper.IsContainerRunningAsync(ContainerName);
            if (!isRunning)
            {
                // Start the container
                Console.WriteLine($"Starting NFS{(int)Version} container...");
                await DockerHelper.ComposeUpAsync(ComposeFilePath, ServiceName, StartupTimeout);
                _containerStarted = true;
            }
            else
            {
                Console.WriteLine($"NFS{(int)Version} container is already running.");
            }

            // Wait for container to be healthy
            Console.WriteLine($"Waiting for NFS{(int)Version} server to be ready...");
            await DockerHelper.WaitForContainerAsync(ContainerName, ReadinessTimeout, waitForHealthy: true);

            // Additional NFS-specific readiness check
            await WaitForNfsReadyAsync();

            IsServerReady = true;
            Console.WriteLine($"NFS{(int)Version} server is ready.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to start NFS{(int)Version} container: {ex.Message}");
            IsServerReady = false;

            // Try to get container logs for debugging
            try
            {
                var logs = await DockerHelper.GetContainerLogsAsync(ContainerName, tail: 50);
                Console.WriteLine($"Container logs:\n{logs}");
            }
            catch
            {
                // Ignore log retrieval errors
            }
        }
    }

    /// <summary>
    /// Cleans up the fixture by stopping the Docker container.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Only stop the container if we started it
        if (_containerStarted && IsDockerAvailable)
        {
            try
            {
                Console.WriteLine($"Stopping NFS{(int)Version} container...");
                await DockerHelper.ComposeDownAsync(ComposeFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to stop container: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Creates a new NFS client configured for this server.
    /// </summary>
    public NfsClient CreateClient()
    {
        return new NfsClient(Version);
    }

    /// <summary>
    /// Creates connection options configured for this server's ports.
    /// </summary>
    public NfsConnectionOptions CreateConnectionOptions()
    {
        return new NfsConnectionOptions
        {
            NfsPort = NfsPort,
            MountPort = MountPort ?? 0,
            UseSecurePort = false  // Docker containers don't require privileged ports
        };
    }

    /// <summary>
    /// Connects a client to this server using the proper port configuration.
    /// </summary>
    /// <param name="client">The client to connect.</param>
    public void ConnectClient(NfsClient client)
    {
        client.Connect(ServerAddress, CreateConnectionOptions());
    }

    /// <summary>
    /// Creates a new NFS client, connects to the server, and mounts the export.
    /// </summary>
    public NfsClient CreateConnectedClient()
    {
        var client = CreateClient();
        var options = new NfsConnectionOptions
        {
            NfsPort = NfsPort,
            MountPort = MountPort ?? 0,
            UseSecurePort = false  // Docker containers don't require privileged ports
        };
        client.Connect(ServerAddress, options);
        client.MountDevice(Export);
        return client;
    }

    /// <summary>
    /// Waits for the NFS service to be ready to accept connections.
    /// Override in derived classes for version-specific checks.
    /// </summary>
    protected virtual async Task WaitForNfsReadyAsync()
    {
        // Try to verify NFS is responding by checking exports
        var maxAttempts = 10;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var result = await DockerHelper.ExecAsync(ContainerName, "showmount -e localhost");
                if (result.Success && result.StandardOutput.Contains("/export"))
                {
                    return;
                }
            }
            catch
            {
                // Ignore and retry
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(1000);
            }
        }

        // If we get here, NFS might still work - the health check passed
        Console.WriteLine("Warning: Could not verify NFS exports, but container is healthy.");
    }

    /// <summary>
    /// Throws if the server is not available, causing the test to fail with a clear message.
    /// In CI environments without Docker, tests will fail with an explanatory message.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Docker or NFS server is not available.</exception>
    public void SkipIfNotAvailable()
    {
        if (!IsDockerAvailable)
        {
            throw new InvalidOperationException(
                "Docker is not available. Integration tests require Docker to be running. " +
                "Install Docker Desktop (Windows/Mac) or Docker Engine (Linux) and ensure it is running.");
        }

        if (!IsServerReady)
        {
            throw new InvalidOperationException(
                $"NFS{(int)Version} server is not ready. The Docker container may have failed to start. " +
                $"Check docker-compose logs for more details.");
        }
    }

    /// <summary>
    /// Checks if the server is available without throwing.
    /// </summary>
    /// <returns>True if both Docker and the NFS server are ready.</returns>
    public bool IsAvailable => IsDockerAvailable && IsServerReady;
}
