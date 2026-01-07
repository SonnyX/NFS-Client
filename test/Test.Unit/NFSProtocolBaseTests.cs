using FluentAssertions;
using NFSLibrary.Protocols.Commons;
using NFSLibrary.Protocols.Commons.Exceptions;
using NFSLibrary.Protocols.Commons.Exceptions.Mount;
using System.Net;
using System.Text;

namespace Test.Unit;

/// <summary>
/// Unit tests for the NFSProtocolBase class.
/// Uses a concrete test implementation to test protected methods.
/// </summary>
public class NFSProtocolBaseTests
{
    #region Test Implementation

    /// <summary>
    /// A testable implementation of NFSProtocolBase that exposes protected methods.
    /// </summary>
    private class TestableNFSProtocol : NFSProtocolBase
    {
        public bool ProtocolConnected { get; set; }
        public bool MountConnected { get; set; }

        protected override bool IsProtocolClientConnected() => ProtocolConnected;
        protected override bool IsMountProtocolClientConnected() => MountConnected;

        // Expose protected static methods for testing
        public static string[] TestParsePathComponents(string path) => ParsePathComponents(path);
        public static string TestGetParentDirectory(string path) => GetParentDirectory(path);
        public static string TestGetItemName(string path) => GetItemName(path);
        public static string TestNormalizePath(string? path) => NormalizePath(path);

        // Expose protected instance methods for testing
        public void TestValidateProtocolConnection() => ValidateProtocolConnection();
        public void TestValidateMountConnection() => ValidateMountConnection();
        public void TestValidateConnection() => ValidateConnection();
        public void TestResetConnectionState() => ResetConnectionState();
        public void TestResetMountState() => ResetMountState();

        // Expose protected fields for testing
        public NFSHandle? RootHandle => _RootDirectoryHandleObject;
        public NFSHandle? CurrentHandle => _CurrentItemHandleObject;
        public string MountedDevice => _MountedDevice;
        public string CurrentItem => _CurrentItem;

        public void SetRootHandle(NFSHandle handle) => _RootDirectoryHandleObject = handle;
        public void SetCurrentHandle(NFSHandle handle) => _CurrentItemHandleObject = handle;
        public void SetMountedDevice(string device) => _MountedDevice = device;
        public void SetCurrentItem(string item) => _CurrentItem = item;

        // Required abstract implementations (minimal stubs)
        public override void Connect(IPAddress address, int userId, int groupId, int clientTimeout,
            Encoding characterEncoding, bool useSecurePort, bool useFhCache, int nfsPort = 0, int mountPort = 0) { }
        public override void Disconnect() { }
        public override int GetBlockSize() => 8192;
        public override List<string> GetExportedDevices() => new();
        public override void MountDevice(string deviceName) { }
        public override void UnMountDevice() { }
        public override List<string> GetItemList(string directoryFullName) => new();
        public override NFSAttributes? GetItemAttributes(string itemFullName, bool throwExceptionIfNotFound) => null;
        public override void CreateDirectory(string directoryFullName, NFSPermission mode) { }
        public override void DeleteDirectory(string directoryFullName) { }
        public override void DeleteFile(string fileFullName) { }
        public override void CreateFile(string fileFullName, NFSPermission mode) { }
        public override int Read(string fileFullName, long offset, int count, ref byte[] buffer) => 0;
        public override void SetFileSize(string fileFullName, long size) { }
        public override int Write(string fileFullName, long offset, int count, byte[] buffer) => 0;
        public override void Move(string oldDir, string oldFile, string newDir, string newFile) { }
        public override bool IsDirectory(string directoryFullName) => false;
        public override void CompleteIO() { }
        public override void CreateSymbolicLink(string linkPath, string targetPath, NFSPermission mode) { }
        public override void CreateHardLink(string linkPath, string targetPath) { }
        public override string ReadSymbolicLink(string linkPath) => "";
        protected override void DisposeProtocolClients() { }
    }

    #endregion

    #region ParsePathComponents Tests

    [Fact]
    public void ParsePathComponents_NullOrEmpty_ReturnsRootArray()
    {
        // Act
        var result1 = TestableNFSProtocol.TestParsePathComponents(null!);
        var result2 = TestableNFSProtocol.TestParsePathComponents(string.Empty);

        // Assert
        result1.Should().BeEquivalentTo(new[] { "." });
        result2.Should().BeEquivalentTo(new[] { "." });
    }

    [Fact]
    public void ParsePathComponents_SingleComponent_ReturnsSingleElement()
    {
        // Act
        var result = TestableNFSProtocol.TestParsePathComponents("folder");

        // Assert
        result.Should().BeEquivalentTo(new[] { "folder" });
    }

    [Fact]
    public void ParsePathComponents_RootPath_ReturnsDotArray()
    {
        // Act
        var result = TestableNFSProtocol.TestParsePathComponents(".");

        // Assert
        result.Should().BeEquivalentTo(new[] { "." });
    }

    [Theory]
    [InlineData(".\\folder", new[] { ".", "folder" })]
    [InlineData(".\\folder\\subfolder", new[] { ".", "folder", "subfolder" })]
    [InlineData(".\\a\\b\\c\\d", new[] { ".", "a", "b", "c", "d" })]
    public void ParsePathComponents_MultipleComponents_SplitsCorrectly(string path, string[] expected)
    {
        // Act
        var result = TestableNFSProtocol.TestParsePathComponents(path);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ParsePathComponents_WithFileName_IncludesFileName()
    {
        // Act
        var result = TestableNFSProtocol.TestParsePathComponents(".\\folder\\file.txt");

        // Assert
        result.Should().BeEquivalentTo(new[] { ".", "folder", "file.txt" });
    }

    [Fact]
    public void ParsePathComponents_TrailingBackslash_CreatesEmptyElement()
    {
        // Act
        var result = TestableNFSProtocol.TestParsePathComponents(".\\folder\\");

        // Assert
        result.Should().BeEquivalentTo(new[] { ".", "folder", "" });
    }

    #endregion

    #region GetParentDirectory Tests

    [Theory]
    [InlineData(".\\folder\\file.txt", ".\\folder")]
    [InlineData(".\\a\\b\\c", ".\\a\\b")]
    [InlineData("folder\\file.txt", "folder")]
    public void GetParentDirectory_ValidPath_ReturnsParent(string path, string expected)
    {
        // Act
        var result = TestableNFSProtocol.TestGetParentDirectory(path);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("file.txt")]
    [InlineData("")]
    public void GetParentDirectory_NoParent_ReturnsDot(string path)
    {
        // Act
        var result = TestableNFSProtocol.TestGetParentDirectory(path);

        // Assert
        result.Should().Be(".");
    }

    #endregion

    #region GetItemName Tests

    [Theory]
    [InlineData(".\\folder\\file.txt", "file.txt")]
    [InlineData(".\\folder", "folder")]
    [InlineData("file.txt", "file.txt")]
    [InlineData(".\\a\\b\\c\\document.pdf", "document.pdf")]
    public void GetItemName_ValidPath_ReturnsFileName(string path, string expected)
    {
        // Act
        var result = TestableNFSProtocol.TestGetItemName(path);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    public void GetItemName_EmptyOrRoot_ReturnsEmptyOrDot(string path)
    {
        // Act
        var result = TestableNFSProtocol.TestGetItemName(path);

        // Assert
        result.Should().BeOneOf("", ".");
    }

    #endregion

    #region NormalizePath Tests

    [Theory]
    [InlineData(null, ".")]
    [InlineData("", ".")]
    public void NormalizePath_NullOrEmpty_ReturnsDot(string? path, string expected)
    {
        // Act
        var result = TestableNFSProtocol.TestNormalizePath(path);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(".", ".")]
    [InlineData(".\\folder", ".\\folder")]
    [InlineData("folder\\file.txt", "folder\\file.txt")]
    public void NormalizePath_ValidPath_ReturnsUnchanged(string path, string expected)
    {
        // Act
        var result = TestableNFSProtocol.TestNormalizePath(path);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateProtocolConnection_WhenConnected_DoesNotThrow()
    {
        // Arrange
        var protocol = new TestableNFSProtocol { ProtocolConnected = true };

        // Act
        var act = () => protocol.TestValidateProtocolConnection();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateProtocolConnection_WhenNotConnected_ThrowsNFSConnectionException()
    {
        // Arrange
        var protocol = new TestableNFSProtocol { ProtocolConnected = false };

        // Act
        var act = () => protocol.TestValidateProtocolConnection();

        // Assert
        act.Should().Throw<NFSConnectionException>()
            .WithMessage("*not connected*");
    }

    [Fact]
    public void ValidateMountConnection_WhenConnected_DoesNotThrow()
    {
        // Arrange
        var protocol = new TestableNFSProtocol { MountConnected = true };

        // Act
        var act = () => protocol.TestValidateMountConnection();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateMountConnection_WhenNotConnected_ThrowsNFSMountConnectionException()
    {
        // Arrange
        var protocol = new TestableNFSProtocol { MountConnected = false };

        // Act
        var act = () => protocol.TestValidateMountConnection();

        // Assert
        act.Should().Throw<NFSMountConnectionException>()
            .WithMessage("*not connected*");
    }

    [Fact]
    public void ValidateConnection_BothConnected_DoesNotThrow()
    {
        // Arrange
        var protocol = new TestableNFSProtocol
        {
            ProtocolConnected = true,
            MountConnected = true
        };

        // Act
        var act = () => protocol.TestValidateConnection();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateConnection_ProtocolNotConnected_ThrowsNFSConnectionException()
    {
        // Arrange
        var protocol = new TestableNFSProtocol
        {
            ProtocolConnected = false,
            MountConnected = true
        };

        // Act
        var act = () => protocol.TestValidateConnection();

        // Assert
        act.Should().Throw<NFSConnectionException>();
    }

    [Fact]
    public void ValidateConnection_MountNotConnected_ThrowsNFSMountConnectionException()
    {
        // Arrange
        var protocol = new TestableNFSProtocol
        {
            ProtocolConnected = true,
            MountConnected = false
        };

        // Act
        var act = () => protocol.TestValidateConnection();

        // Assert
        act.Should().Throw<NFSMountConnectionException>();
    }

    #endregion

    #region State Reset Tests

    [Fact]
    public void ResetConnectionState_ClearsAllStateFields()
    {
        // Arrange
        var protocol = new TestableNFSProtocol();
        protocol.SetRootHandle(new NFSHandle(new byte[] { 1, 2, 3 }, 3));
        protocol.SetCurrentHandle(new NFSHandle(new byte[] { 4, 5, 6 }, 3));
        protocol.SetMountedDevice("/export/test");
        protocol.SetCurrentItem(".\\folder\\file.txt");

        // Act
        protocol.TestResetConnectionState();

        // Assert
        protocol.RootHandle.Should().BeNull();
        protocol.CurrentHandle.Should().BeNull();
        protocol.MountedDevice.Should().BeEmpty();
        protocol.CurrentItem.Should().BeEmpty();
    }

    [Fact]
    public void ResetMountState_ClearsAllStateFields()
    {
        // Arrange
        var protocol = new TestableNFSProtocol();
        protocol.SetRootHandle(new NFSHandle(new byte[] { 1, 2, 3 }, 3));
        protocol.SetCurrentHandle(new NFSHandle(new byte[] { 4, 5, 6 }, 3));
        protocol.SetMountedDevice("/export/test");
        protocol.SetCurrentItem(".\\folder\\file.txt");

        // Act
        protocol.TestResetMountState();

        // Assert
        protocol.RootHandle.Should().BeNull();
        protocol.CurrentHandle.Should().BeNull();
        protocol.MountedDevice.Should().BeEmpty();
        protocol.CurrentItem.Should().BeEmpty();
    }

    #endregion

    #region IDisposable Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var protocol = new TestableNFSProtocol();

        // Act & Assert - should not throw
        protocol.Dispose();
        protocol.Dispose();
    }

    [Fact]
    public void UsingStatement_DisposesCorrectly()
    {
        // Arrange & Act & Assert - should not throw
        using (var protocol = new TestableNFSProtocol())
        {
            protocol.Should().NotBeNull();
        }
    }

    #endregion
}
