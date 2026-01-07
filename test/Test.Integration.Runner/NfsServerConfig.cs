using System.Net;
using NFSLibrary;

namespace Test.Integration.Runner;

/// <summary>
/// Configuration for an NFS server used in testing.
/// </summary>
public class NfsServerConfig
{
    /// <summary>
    /// Gets the name of this server configuration.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the NFS version.
    /// </summary>
    public required NfsVersion Version { get; init; }

    /// <summary>
    /// Gets the NFS port.
    /// </summary>
    public required int NfsPort { get; init; }

    /// <summary>
    /// Gets the mount port (NFSv3 only).
    /// </summary>
    public int? MountPort { get; init; }

    /// <summary>
    /// Gets the container name.
    /// </summary>
    public required string ContainerName { get; init; }

    /// <summary>
    /// Gets the service name for docker-compose.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the compose file path.
    /// </summary>
    public required string ComposeFilePath { get; init; }

    /// <summary>
    /// Gets the export path.
    /// </summary>
    public string Export => "/export";

    /// <summary>
    /// Gets the server address.
    /// </summary>
    public IPAddress ServerAddress => IPAddress.Loopback;

    /// <summary>
    /// Gets or sets whether the server is ready.
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    /// Creates the connection options for this server.
    /// </summary>
    public NfsConnectionOptions CreateConnectionOptions()
    {
        return new NfsConnectionOptions
        {
            NfsPort = NfsPort,
            MountPort = MountPort ?? 0,
            UseSecurePort = false
        };
    }

    /// <summary>
    /// Creates a connected NFS client for this server.
    /// </summary>
    public NfsClient CreateConnectedClient()
    {
        var client = new NfsClient(Version);
        client.Connect(ServerAddress, CreateConnectionOptions());
        client.MountDevice(Export);
        return client;
    }

    /// <summary>
    /// Gets the NFSv3 server configuration.
    /// </summary>
    public static NfsServerConfig CreateV3Config() => new()
    {
        Name = "NFSv3",
        Version = NfsVersion.V3,
        NfsPort = 22049,
        MountPort = 22767,
        ContainerName = "nfs-integration-v3",
        ServiceName = "nfsv3-server",
        ComposeFilePath = DockerHelper.GetComposeFilePath(3)
    };

    /// <summary>
    /// Gets the NFSv4 server configuration.
    /// </summary>
    public static NfsServerConfig CreateV4Config() => new()
    {
        Name = "NFSv4",
        Version = NfsVersion.V4,
        NfsPort = 32049,
        MountPort = null,
        ContainerName = "nfs-integration-v4",
        ServiceName = "nfsv4-server",
        ComposeFilePath = DockerHelper.GetComposeFilePath(4)
    };
}
