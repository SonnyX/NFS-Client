using System.Net;
using System.Text;
using FluentAssertions;
using NFSLibrary;
using NFSLibrary.Protocols.Commons;
using Test.Integration.Fixtures;
using Test.Integration.Helpers;

namespace Test.Integration.Tests;

/// <summary>
/// Base class for NFS integration tests.
/// Contains common test methods that work across all NFS versions.
/// </summary>
public abstract class NfsIntegrationTestBase<TFixture> where TFixture : NfsServerFixture
{
    protected readonly TFixture Fixture;

    protected NfsIntegrationTestBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    #region Connection Tests

    [Fact]
    [Trait("Category", "Integration")]
    public void Connect_EstablishesConnection()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateClient();
        Fixture.ConnectClient(client);

        client.IsConnected.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Connect_Disconnect_CanReconnect()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateClient();

        // First connection
        Fixture.ConnectClient(client);
        client.IsConnected.Should().BeTrue();

        // Disconnect
        client.Disconnect();
        client.IsConnected.Should().BeFalse();

        // Reconnect
        Fixture.ConnectClient(client);
        client.IsConnected.Should().BeTrue();
    }

    #endregion

    #region Mount Tests

    [Fact]
    [Trait("Category", "Integration")]
    public void GetExportedDevices_ReturnsExports()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateClient();
        Fixture.ConnectClient(client);

        var exports = client.GetExportedDevices();

        exports.Should().NotBeNull();
        exports.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void MountDevice_MountsExport()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateClient();
        Fixture.ConnectClient(client);

        client.MountDevice(Fixture.Export);

        client.IsMounted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void UnMountDevice_UnmountsExport()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        client.IsMounted.Should().BeTrue();

        client.UnMountDevice();

        client.IsMounted.Should().BeFalse();
    }

    #endregion

    #region Directory Tests

    [Fact]
    [Trait("Category", "Integration")]
    public void GetItemList_RootDirectory_ReturnsItems()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();

        var items = client.GetItemList(".");

        items.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void CreateDirectory_CreatesNewDirectory()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var dirName = TestDataGenerator.GenerateDirectoryName();

        try
        {
            client.CreateDirectory($".\\{dirName}");

            var items = client.GetItemList(".");
            items.Should().Contain(dirName);
        }
        finally
        {
            // Cleanup
            try { client.DeleteDirectory($".\\{dirName}"); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void DeleteDirectory_RemovesDirectory()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var dirName = TestDataGenerator.GenerateDirectoryName();

        // Create first
        client.CreateDirectory($".\\{dirName}");

        // Then delete
        client.DeleteDirectory($".\\{dirName}");

        var items = client.GetItemList(".");
        items.Should().NotContain(dirName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void IsDirectory_ForDirectory_ReturnsTrue()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var dirName = TestDataGenerator.GenerateDirectoryName();

        try
        {
            client.CreateDirectory($".\\{dirName}");

            var isDir = client.IsDirectory($".\\{dirName}");

            isDir.Should().BeTrue();
        }
        finally
        {
            try { client.DeleteDirectory($".\\{dirName}"); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void IsDirectory_ForFile_ReturnsFalse()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            var isDir = client.IsDirectory($".\\{fileName}");

            isDir.Should().BeFalse();
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    #endregion

    #region File Tests

    [Fact]
    [Trait("Category", "Integration")]
    public void CreateFile_CreatesEmptyFile()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            var exists = client.FileExists($".\\{fileName}");
            exists.Should().BeTrue();
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void DeleteFile_RemovesFile()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        // Create first
        client.CreateFile($".\\{fileName}");

        // Then delete
        client.DeleteFile($".\\{fileName}");

        var exists = client.FileExists($".\\{fileName}");
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void FileExists_ForExistingFile_ReturnsTrue()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            var exists = client.FileExists($".\\{fileName}");

            exists.Should().BeTrue();
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void FileExists_ForNonExistentFile_ReturnsFalse()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        var exists = client.FileExists($".\\{fileName}");

        exists.Should().BeFalse();
    }

    #endregion

    #region Read/Write Tests

    [Fact]
    [Trait("Category", "Integration")]
    public void WriteAndRead_SmallContent_RoundTrips()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content = TestDataGenerator.GenerateSmallContent();

        try
        {
            // Write
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            // Read
            var buffer = new byte[content.Length];
            client.Read($".\\{fileName}", 0, content.Length, ref buffer);

            buffer.Should().BeEquivalentTo(content);
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void WriteAndRead_TextContent_RoundTrips()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var textContent = "Hello, NFS World! This is a test string.";
        var content = Encoding.UTF8.GetBytes(textContent);

        try
        {
            // Write
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            // Read
            var buffer = new byte[content.Length];
            client.Read($".\\{fileName}", 0, content.Length, ref buffer);

            var readText = Encoding.UTF8.GetString(buffer);
            readText.Should().Be(textContent);
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Write_ToExistingFile_OverwritesContent()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content1 = Encoding.UTF8.GetBytes("First content");
        var content2 = Encoding.UTF8.GetBytes("Second content, longer");

        try
        {
            client.CreateFile($".\\{fileName}");

            // Write first content
            client.Write($".\\{fileName}", 0, content1.Length, content1);

            // Overwrite with second content
            client.SetFileSize($".\\{fileName}", 0); // Truncate
            client.Write($".\\{fileName}", 0, content2.Length, content2);

            // Read back
            var buffer = new byte[content2.Length];
            client.Read($".\\{fileName}", 0, content2.Length, ref buffer);

            var readText = Encoding.UTF8.GetString(buffer);
            readText.Should().Be("Second content, longer");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    #endregion

    #region Attributes Tests

    [Fact]
    [Trait("Category", "Integration")]
    public void GetItemAttributes_ForFile_ReturnsAttributes()
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
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void GetItemAttributes_ForDirectory_ReturnsDirectoryType()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var dirName = TestDataGenerator.GenerateDirectoryName();

        try
        {
            client.CreateDirectory($".\\{dirName}");

            var attrs = client.GetItemAttributes($".\\{dirName}");

            attrs.Should().NotBeNull();
            attrs.NFSType.Should().Be(NFSItemTypes.NFDIR);
        }
        finally
        {
            try { client.DeleteDirectory($".\\{dirName}"); } catch { }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void SetFileSize_ChangesFileSize()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content = TestDataGenerator.GenerateSmallContent();

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            // Get initial size
            var attrs = client.GetItemAttributes($".\\{fileName}");
            ((long)attrs.Size).Should().Be(content.Length);

            // Truncate to half
            var newSize = content.Length / 2;
            client.SetFileSize($".\\{fileName}", newSize);

            // Verify new size
            attrs = client.GetItemAttributes($".\\{fileName}");
            ((long)attrs.Size).Should().Be(newSize);
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }
    }

    #endregion

    #region Move/Rename Tests

    [Fact]
    [Trait("Category", "Integration")]
    public void Move_RenamesFile()
    {
        Fixture.SkipIfNotAvailable();

        using var client = Fixture.CreateConnectedClient();
        var originalName = TestDataGenerator.GenerateFileName("original");
        var newName = TestDataGenerator.GenerateFileName("renamed");

        try
        {
            client.CreateFile($".\\{originalName}");

            client.Move($".\\{originalName}", $".\\{newName}");

            client.FileExists($".\\{originalName}").Should().BeFalse();
            client.FileExists($".\\{newName}").Should().BeTrue();
        }
        finally
        {
            try { client.DeleteFile($".\\{originalName}"); } catch { }
            try { client.DeleteFile($".\\{newName}"); } catch { }
        }
    }

    #endregion
}
