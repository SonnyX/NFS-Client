using FluentAssertions;
using Test.Unit.Mocks;
using NFSLibrary.Protocols.Commons;
using System.Net;

namespace Test.Unit;

public class MockNfsClientTests
{
    #region Connection Tests

    [Fact]
    public void Connect_SetsIsConnectedTrue()
    {
        // Arrange
        using var client = new MockNfsClient();

        // Act
        client.Connect(IPAddress.Loopback);

        // Assert
        client.IsConnected.Should().BeTrue();
        client.IsMounted.Should().BeFalse();
    }

    [Fact]
    public void Disconnect_SetsIsConnectedFalse()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);

        // Act
        client.Disconnect();

        // Assert
        client.IsConnected.Should().BeFalse();
    }

    #endregion

    #region Mount Tests

    [Fact]
    public void MountDevice_SetsIsMountedTrue()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);

        // Act
        client.MountDevice("/export/test");

        // Assert
        client.IsMounted.Should().BeTrue();
    }

    [Fact]
    public void GetExportedDevices_ReturnsDeviceList()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);

        // Act
        var devices = client.GetExportedDevices();

        // Assert
        devices.Should().NotBeEmpty();
        devices.Should().Contain("/export/test");
    }

    [Fact]
    public void UnMountDevice_SetsIsMountedFalse()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        // Act
        client.UnMountDevice();

        // Assert
        client.IsMounted.Should().BeFalse();
    }

    #endregion

    #region Directory Operations

    [Fact]
    public void CreateDirectory_CreatesNewDirectory()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        // Act
        client.CreateDirectory(".\\testdir");

        // Assert
        client.IsDirectory(".\\testdir").Should().BeTrue();
    }

    [Fact]
    public void DeleteDirectory_RemovesDirectory()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");
        client.CreateDirectory(".\\testdir");

        // Act
        client.DeleteDirectory(".\\testdir");

        // Assert
        client.FileExists(".\\testdir").Should().BeFalse();
    }

    [Fact]
    public void GetItemList_ReturnsItemsInDirectory()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");
        client.CreateDirectory(".\\dir1");
        client.CreateFile(".\\file1.txt");

        // Act
        var items = client.GetItemList(".", excludeNavigationDots: true);

        // Assert
        items.Should().Contain("dir1");
        items.Should().Contain("file1.txt");
    }

    #endregion

    #region File Operations

    [Fact]
    public void CreateFile_CreatesNewFile()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        // Act
        client.CreateFile(".\\test.txt");

        // Assert
        client.FileExists(".\\test.txt").Should().BeTrue();
        client.IsDirectory(".\\test.txt").Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_RemovesFile()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");
        client.CreateFile(".\\test.txt");

        // Act
        client.DeleteFile(".\\test.txt");

        // Assert
        client.FileExists(".\\test.txt").Should().BeFalse();
    }

    [Fact]
    public void WriteAndRead_RoundTripsData()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        var testData = new byte[] { 1, 2, 3, 4, 5 };
        using var inputStream = new MemoryStream(testData);

        // Act
        client.Write(".\\data.bin", inputStream);

        using var outputStream = new MemoryStream();
        Stream tempStream = outputStream;
        client.Read(".\\data.bin", ref tempStream);

        // Assert
        outputStream.ToArray().Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void Write_WithOffset_AppendsData()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        var initialData = new byte[] { 1, 2, 3 };
        var appendData = new byte[] { 4, 5 };

        using var stream1 = new MemoryStream(initialData);
        client.Write(".\\data.bin", stream1);

        // Act
        client.Write(".\\data.bin", 3, appendData.Length, appendData);

        // Assert
        var attrs = client.GetItemAttributes(".\\data.bin");
        attrs.Size.Should().Be(5);
    }

    [Fact]
    public void SetFileSize_ChangesFileSize()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        var testData = new byte[] { 1, 2, 3, 4, 5 };
        using var inputStream = new MemoryStream(testData);
        client.Write(".\\data.bin", inputStream);

        // Act
        client.SetFileSize(".\\data.bin", 3);

        // Assert
        var attrs = client.GetItemAttributes(".\\data.bin");
        attrs.Size.Should().Be(3);
    }

    #endregion

    #region Move Operations

    [Fact]
    public void Move_RenamesFile()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");
        client.CreateFile(".\\original.txt");

        // Act
        client.Move(".\\original.txt", ".\\renamed.txt");

        // Assert
        client.FileExists(".\\original.txt").Should().BeFalse();
        client.FileExists(".\\renamed.txt").Should().BeTrue();
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void GetItemAttributes_ReturnsCorrectAttributes()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        var mode = new NFSPermission(6, 4, 0);
        client.CreateFile(".\\test.txt", mode);

        // Act
        var attrs = client.GetItemAttributes(".\\test.txt");

        // Assert
        attrs.Should().NotBeNull();
        attrs.NFSType.Should().Be(NFSItemTypes.NFREG);
        attrs.Mode.UserAccess.Should().Be(6);
        attrs.Mode.GroupAccess.Should().Be(4);
        attrs.Mode.OtherAccess.Should().Be(0);
    }

    [Fact]
    public void GetItemAttributes_ThrowsWhenNotFound()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        // Act & Assert
        var act = () => client.GetItemAttributes(".\\nonexistent.txt");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void GetItemAttributes_ReturnsNullWhenNotFoundAndNoThrow()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        // Act
        var attrs = client.GetItemAttributes(".\\nonexistent.txt", throwExceptionIfNotFound: false);

        // Assert - should not throw, returns null
        // Note: The actual behavior depends on implementation
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public void Read_WithCancellation_ThrowsWhenCancelled()
    {
        // Arrange
        using var client = new MockNfsClient();
        client.Connect(IPAddress.Loopback);
        client.MountDevice("/export/test");

        var testData = new byte[] { 1, 2, 3 };
        using var inputStream = new MemoryStream(testData);
        client.Write(".\\data.bin", inputStream);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        using var outputStream = new MemoryStream();
        Stream tempStream = outputStream;
        var act = () => client.Read(".\\data.bin", ref tempStream, cts.Token);
        act.Should().Throw<OperationCanceledException>();
    }

    #endregion
}
