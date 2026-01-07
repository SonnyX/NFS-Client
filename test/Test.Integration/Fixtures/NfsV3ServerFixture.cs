using NFSLibrary;
using Test.Integration.Helpers;

namespace Test.Integration.Fixtures;

/// <summary>
/// Test fixture for NFSv3 server.
/// </summary>
public class NfsV3ServerFixture : NfsServerFixture
{
    /// <inheritdoc />
    protected override string ComposeFilePath =>
        DockerHelper.GetComposeFilePath(3);

    /// <inheritdoc />
    protected override string ServiceName => "nfsv3-server";

    /// <inheritdoc />
    protected override string ContainerName => "nfs-integration-v3";

    /// <inheritdoc />
    public override int NfsPort => 22049;

    /// <inheritdoc />
    public override int? PortmapperPort => 20111;

    /// <inheritdoc />
    public override int? MountPort => 22767;

    /// <inheritdoc />
    public override NfsVersion Version => NfsVersion.V3;
}

/// <summary>
/// Collection definition for NFSv3 tests.
/// Ensures the NFSv3 server fixture is shared across all tests in the collection.
/// </summary>
[CollectionDefinition("NFSv3")]
public class NfsV3Collection : ICollectionFixture<NfsV3ServerFixture>
{
}
