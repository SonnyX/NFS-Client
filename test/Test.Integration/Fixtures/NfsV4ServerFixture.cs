using NFSLibrary;
using Test.Integration.Helpers;

namespace Test.Integration.Fixtures;

/// <summary>
/// Test fixture for NFSv4 server.
/// </summary>
public class NfsV4ServerFixture : NfsServerFixture
{
    /// <inheritdoc />
    protected override string ComposeFilePath =>
        DockerHelper.GetComposeFilePath(4);

    /// <inheritdoc />
    protected override string ServiceName => "nfsv4-server";

    /// <inheritdoc />
    protected override string ContainerName => "nfs-integration-v4";

    /// <inheritdoc />
    public override int NfsPort => 32049;

    /// <inheritdoc />
    public override int? PortmapperPort => null; // NFSv4 doesn't use portmapper

    /// <inheritdoc />
    public override NfsVersion Version => NfsVersion.V4;

    /// <summary>
    /// Gets the NFSv4 pseudo-filesystem root export.
    /// </summary>
    public string RootExport => "/export";

    /// <summary>
    /// Waits for the NFSv4 server to be ready.
    /// The Docker container is configured with a 10-second grace period via NFS_SERVER_FLAGS.
    /// </summary>
    protected override async Task WaitForNfsReadyAsync()
    {
        // Verify nfsd threads are running
        var maxAttempts = 10;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var result = await DockerHelper.ExecAsync(ContainerName, "cat /proc/fs/nfsd/threads");
                if (result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput))
                {
                    var threadCount = result.StandardOutput.Trim();
                    if (int.TryParse(threadCount, out int count) && count > 0)
                    {
                        // Wait for grace period to end
                        // The erichough/nfs-server image has a 90-second grace period
                        Console.WriteLine("Waiting for NFSv4 grace period (90 seconds)...");
                        await Task.Delay(92000);
                        return;
                    }
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

        Console.WriteLine("Warning: Could not verify NFSv4 nfsd threads, but container is healthy.");
    }
}

/// <summary>
/// Collection definition for NFSv4 tests.
/// Ensures the NFSv4 server fixture is shared across all tests in the collection.
/// </summary>
[CollectionDefinition("NFSv4")]
public class NfsV4Collection : ICollectionFixture<NfsV4ServerFixture>
{
}
