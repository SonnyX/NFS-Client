using FluentAssertions;
using NFSLibrary;
using NFSLibrary.Protocols.Commons;
using Test.Integration.Fixtures;
using Test.Integration.Helpers;

namespace Test.Integration.Tests;

/// <summary>
/// Integration tests for NFSv3 protocol.
/// </summary>
[Collection("NFSv3")]
[Trait("Category", "Integration")]
[Trait("NfsVersion", "3")]
public class NfsV3IntegrationTests : NfsIntegrationTestBase<NfsV3ServerFixture>
{
    public NfsV3IntegrationTests(NfsV3ServerFixture fixture) : base(fixture)
    {
    }

    #region NFSv3-Specific Tests

    [Fact]
    public void Version_IsV3()
    {
        Fixture.Version.Should().Be(NfsVersion.V3);
    }

    [Fact]
    public void NfsPort_Is22049()
    {
        Fixture.NfsPort.Should().Be(22049);
    }

    [Fact]
    public void PortmapperPort_Is20111()
    {
        Fixture.PortmapperPort.Should().Be(20111);
    }

    [Fact]
    public void BlockSize_IsNegotiated()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();

        // NFSv3 negotiates block size via FSINFO
        client.BlockSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LargeFile_CanBeWrittenAndRead()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        // NFSv3 supports 64-bit file sizes - test with 100KB
        var content = TestDataGenerator.GeneratePatternedContent(100 * 1024);

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            // Read back and verify
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
    public void PartialRead_ReturnsCorrectData()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content = TestDataGenerator.GeneratePatternedContent(4096);

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            // Read middle portion
            var offset = 1024;
            var length = 512;
            var buffer = new byte[length];
            client.Read($".\\{fileName}", offset, length, ref buffer);

            // Verify the pattern at the offset
            for (int i = 0; i < length; i++)
            {
                buffer[i].Should().Be((byte)((offset + i) % 256));
            }
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    [Fact]
    public void AppendWrite_AddsToEndOfFile()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content1 = new byte[] { 1, 2, 3, 4, 5 };
        var content2 = new byte[] { 6, 7, 8, 9, 10 };

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content1.Length, content1);
            client.Write($".\\{fileName}", content1.Length, content2.Length, content2);

            // Read all
            var buffer = new byte[10];
            client.Read($".\\{fileName}", 0, 10, ref buffer);

            buffer.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    [Fact]
    public void NestedDirectory_CanBeCreatedAndTraversed()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var dir1 = TestDataGenerator.GenerateDirectoryName();
        var dir2 = TestDataGenerator.GenerateDirectoryName();

        try
        {
            client.CreateDirectory($".\\{dir1}");
            client.CreateDirectory($".\\{dir1}\\{dir2}");

            var items = client.GetItemList($".\\{dir1}");
            items.Should().Contain(dir2);
        }
        finally
        {
            try { client.DeleteDirectory($".\\{dir1}\\{dir2}"); } catch { }
            try { client.DeleteDirectory($".\\{dir1}"); } catch { }
        }
    }

    #endregion
}
