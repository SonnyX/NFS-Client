using FluentAssertions;
using NFSLibrary;
using NFSLibrary.Protocols.Commons;
using Test.Integration.Fixtures;
using Test.Integration.Helpers;

namespace Test.Integration.Tests;

/// <summary>
/// Integration tests for NFSv4 protocol.
/// </summary>
[Collection("NFSv4")]
[Trait("Category", "Integration")]
[Trait("NfsVersion", "4")]
public class NfsV4IntegrationTests : NfsIntegrationTestBase<NfsV4ServerFixture>
{
    public NfsV4IntegrationTests(NfsV4ServerFixture fixture) : base(fixture)
    {
    }

    #region NFSv4-Specific Tests

    [Fact]
    public void Version_IsV4()
    {
        Fixture.Version.Should().Be(NfsVersion.V4);
    }

    [Fact]
    public void NfsPort_Is32049()
    {
        Fixture.NfsPort.Should().Be(32049);
    }

    [Fact]
    public void PortmapperPort_IsNull()
    {
        // NFSv4 doesn't use portmapper
        Fixture.PortmapperPort.Should().BeNull();
    }

    [Fact]
    public void RootExport_IsAvailable()
    {
        Fixture.RootExport.Should().Be("/export");
    }

    [Fact]
    public void Session_IsEstablished()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateClient();
        Fixture.ConnectClient(client);

        // NFSv4 establishes a session on connect
        client.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void LargeFile_CanBeWrittenAndRead()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        // NFSv4 supports large files - test with 256KB
        var content = TestDataGenerator.GeneratePatternedContent(256 * 1024);

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            // Read back in chunks and verify
            var buffer = new byte[content.Length];
            client.Read($".\\{fileName}", 0, content.Length, ref buffer);

            TestDataGenerator.VerifyPatternedContent(buffer, content.Length).Should().BeTrue();
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    [Fact]
    public void AtomicRename_Works()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var originalName = TestDataGenerator.GenerateFileName("atomic");
        var newName = TestDataGenerator.GenerateFileName("renamed");
        var content = new byte[] { 1, 2, 3, 4, 5 };

        try
        {
            // Create file with content
            client.CreateFile($".\\{originalName}");
            client.Write($".\\{originalName}", 0, content.Length, content);

            // Rename atomically
            client.Move($".\\{originalName}", $".\\{newName}");

            // Verify content is preserved
            var buffer = new byte[content.Length];
            client.Read($".\\{newName}", 0, content.Length, ref buffer);

            buffer.Should().BeEquivalentTo(content);
        }
        finally
        {
            try { client.DeleteFile($".\\{originalName}"); } catch { }
            try { client.DeleteFile($".\\{newName}"); } catch { }
        }
    }

    [Fact]
    public void DeepNestedPath_CanBeAccessed()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var dirs = new[]
        {
            TestDataGenerator.GenerateDirectoryName("level1"),
            TestDataGenerator.GenerateDirectoryName("level2"),
            TestDataGenerator.GenerateDirectoryName("level3")
        };
        var fileName = TestDataGenerator.GenerateFileName("deep");

        try
        {
            // Create nested structure
            client.CreateDirectory($".\\{dirs[0]}");
            client.CreateDirectory($".\\{dirs[0]}\\{dirs[1]}");
            client.CreateDirectory($".\\{dirs[0]}\\{dirs[1]}\\{dirs[2]}");

            // Create file at deepest level
            var path = $".\\{dirs[0]}\\{dirs[1]}\\{dirs[2]}\\{fileName}";
            client.CreateFile(path);

            // Verify it exists
            client.FileExists(path).Should().BeTrue();
        }
        finally
        {
            try
            {
                client.DeleteFile($".\\{dirs[0]}\\{dirs[1]}\\{dirs[2]}\\{fileName}");
                client.DeleteDirectory($".\\{dirs[0]}\\{dirs[1]}\\{dirs[2]}");
                client.DeleteDirectory($".\\{dirs[0]}\\{dirs[1]}");
                client.DeleteDirectory($".\\{dirs[0]}");
            }
            catch { }
        }
    }

    [Fact]
    public void FileAttributes_IncludeAllFields()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            var attrs = client.GetItemAttributes($".\\{fileName}");

            attrs.Should().NotBeNull();
            attrs.NFSType.Should().Be(NFSItemTypes.NFREG);
            attrs.Size.Should().Be(0); // Empty file
            attrs.Mode.Should().NotBeNull();
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    #endregion
}
