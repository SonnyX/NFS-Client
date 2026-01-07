using FluentAssertions;
using NFSLibrary.Protocols.Commons;

namespace Test.Unit;

/// <summary>
/// Unit tests for the NFSAttributes class.
/// </summary>
public class NFSAttributesTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_StoresAllValues()
    {
        // Arrange
        int createTime = 1700000000;
        int accessTime = 1700000100;
        int modifyTime = 1700000200;
        var type = NFSItemTypes.NFREG;
        var mode = new NFSPermission(7, 5, 5);
        long size = 1024;
        var handle = new byte[] { 1, 2, 3, 4 };

        // Act
        var attrs = new NFSAttributes(createTime, accessTime, modifyTime, type, mode, size, handle);

        // Assert
        attrs.NFSType.Should().Be(type);
        attrs.Mode.Should().Be(mode);
        attrs.Size.Should().Be(size);
        attrs.Handle.Should().BeEquivalentTo(handle);
    }

    [Fact]
    public void Constructor_ConvertsUnixTimestampToDateTime_CreateTime()
    {
        // Arrange - Unix timestamp for 2023-11-14 22:13:20 UTC
        int timestamp = 1700000000;
        var expectedDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddSeconds(timestamp);

        // Act
        var attrs = new NFSAttributes(timestamp, 0, 0, NFSItemTypes.NFREG, new NFSPermission(), 0, new byte[4]);

        // Assert
        attrs.CreateDateTime.Should().Be(expectedDate);
    }

    [Fact]
    public void Constructor_ConvertsUnixTimestampToDateTime_AccessTime()
    {
        // Arrange
        int timestamp = 1700000100;
        var expectedDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddSeconds(timestamp);

        // Act
        var attrs = new NFSAttributes(0, timestamp, 0, NFSItemTypes.NFREG, new NFSPermission(), 0, new byte[4]);

        // Assert
        attrs.LastAccessedDateTime.Should().Be(expectedDate);
    }

    [Fact]
    public void Constructor_ConvertsUnixTimestampToDateTime_ModifyTime()
    {
        // Arrange
        int timestamp = 1700000200;
        var expectedDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddSeconds(timestamp);

        // Act
        var attrs = new NFSAttributes(0, 0, timestamp, NFSItemTypes.NFREG, new NFSPermission(), 0, new byte[4]);

        // Assert
        attrs.ModifiedDateTime.Should().Be(expectedDate);
    }

    #endregion

    #region Timestamp Edge Cases

    [Fact]
    public void Constructor_ZeroTimestamp_ReturnsUnixEpoch()
    {
        // Arrange
        var expectedDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        // Act
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG, new NFSPermission(), 0, new byte[4]);

        // Assert
        attrs.CreateDateTime.Should().Be(expectedDate);
        attrs.LastAccessedDateTime.Should().Be(expectedDate);
        attrs.ModifiedDateTime.Should().Be(expectedDate);
    }

    [Fact]
    public void Constructor_MaxInt32Timestamp_DoesNotThrow()
    {
        // Arrange - Year 2038 problem boundary
        int maxTimestamp = int.MaxValue; // 2147483647 = 2038-01-19

        // Act
        var act = () => new NFSAttributes(maxTimestamp, maxTimestamp, maxTimestamp,
            NFSItemTypes.NFREG, new NFSPermission(), 0, new byte[4]);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NegativeTimestamp_HandlesCorrectly()
    {
        // Arrange - Negative timestamps represent dates before Unix epoch
        int negativeTimestamp = -86400; // One day before epoch

        // Act
        var attrs = new NFSAttributes(negativeTimestamp, 0, 0, NFSItemTypes.NFREG, new NFSPermission(), 0, new byte[4]);

        // Assert - Should be 1969-12-31
        attrs.CreateDateTime.Should().BeBefore(new DateTime(1970, 1, 1));
    }

    #endregion

    #region NFSType Tests

    [Theory]
    [InlineData(NFSItemTypes.NFNON)]
    [InlineData(NFSItemTypes.NFREG)]
    [InlineData(NFSItemTypes.NFDIR)]
    [InlineData(NFSItemTypes.NFBLK)]
    [InlineData(NFSItemTypes.NFCHR)]
    [InlineData(NFSItemTypes.NFLNK)]
    [InlineData(NFSItemTypes.NFSOCK)]
    [InlineData(NFSItemTypes.NFFIFO)]
    public void NFSType_AllTypes_ReturnCorrectValue(NFSItemTypes type)
    {
        // Arrange
        var attrs = new NFSAttributes(0, 0, 0, type, new NFSPermission(), 0, new byte[4]);

        // Assert
        attrs.NFSType.Should().Be(type);
    }

    #endregion

    #region Size Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(1048576)] // 1 MB
    [InlineData(1073741824)] // 1 GB
    [InlineData(long.MaxValue)]
    public void Size_VariousSizes_ReturnsCorrectValue(long size)
    {
        // Arrange
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG, new NFSPermission(), size, new byte[4]);

        // Assert
        attrs.Size.Should().Be(size);
    }

    #endregion

    #region Handle Tests

    [Fact]
    public void Handle_Constructor_ClonesInputArray()
    {
        // Arrange
        var originalHandle = new byte[] { 1, 2, 3, 4 };

        // Act
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG, new NFSPermission(), 0, originalHandle);

        // Modify original array
        originalHandle[0] = 99;

        // Assert - Internal handle should be unchanged
        attrs.Handle[0].Should().Be(1);
    }

    [Fact]
    public void Handle_EmptyArray_HandledCorrectly()
    {
        // Arrange
        var emptyHandle = Array.Empty<byte>();

        // Act
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG, new NFSPermission(), 0, emptyHandle);

        // Assert
        attrs.Handle.Should().BeEmpty();
    }

    [Fact]
    public void Handle_LargeArray_StoredCorrectly()
    {
        // Arrange
        var largeHandle = new byte[128];
        for (int i = 0; i < largeHandle.Length; i++)
        {
            largeHandle[i] = (byte)(i % 256);
        }

        // Act
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG, new NFSPermission(), 0, largeHandle);

        // Assert
        attrs.Handle.Should().BeEquivalentTo(largeHandle);
    }

    #endregion

    #region Mode Tests

    [Fact]
    public void Mode_ReturnsStoredPermission()
    {
        // Arrange
        var mode = new NFSPermission(6, 4, 0);
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG, mode, 0, new byte[4]);

        // Assert
        attrs.Mode.Should().Be(mode);
        attrs.Mode.UserAccess.Should().Be(6);
        attrs.Mode.GroupAccess.Should().Be(4);
        attrs.Mode.OtherAccess.Should().Be(0);
    }

    [Theory]
    [InlineData(7, 7, 7)] // Full permissions
    [InlineData(6, 4, 4)] // Typical file permissions
    [InlineData(7, 5, 5)] // Typical directory permissions
    [InlineData(0, 0, 0)] // No permissions
    public void Mode_VariousPermissions_StoredCorrectly(byte user, byte group, byte other)
    {
        // Arrange
        var mode = new NFSPermission(user, group, other);
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG, mode, 0, new byte[4]);

        // Assert
        attrs.Mode.UserAccess.Should().Be(user);
        attrs.Mode.GroupAccess.Should().Be(group);
        attrs.Mode.OtherAccess.Should().Be(other);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ContainsAllRelevantInformation()
    {
        // Arrange
        var attrs = new NFSAttributes(
            1700000000, 1700000100, 1700000200,
            NFSItemTypes.NFREG,
            new NFSPermission(7, 5, 5),
            1024,
            new byte[] { 0xAB, 0xCD });

        // Act
        var result = attrs.ToString();

        // Assert
        result.Should().Contain("CDateTime:");
        result.Should().Contain("ADateTime:");
        result.Should().Contain("MDateTime:");
        result.Should().Contain("Type: NFREG");
        result.Should().Contain("Mode: 755");
        result.Should().Contain("Size: 1024");
        result.Should().Contain("Handle:");
    }

    [Fact]
    public void ToString_HandleFormattedAsHex()
    {
        // Arrange
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG,
            new NFSPermission(), 0, new byte[] { 0xFF, 0x00, 0xAB });

        // Act
        var result = attrs.ToString();

        // Assert
        result.Should().Contain("FF");
        result.Should().Contain("0");
        result.Should().Contain("AB");
    }

    [Fact]
    public void ToString_EmptyHandle_DoesNotThrow()
    {
        // Arrange
        var attrs = new NFSAttributes(0, 0, 0, NFSItemTypes.NFREG,
            new NFSPermission(), 0, Array.Empty<byte>());

        // Act
        var act = () => attrs.ToString();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(NFSItemTypes.NFDIR, "NFDIR")]
    [InlineData(NFSItemTypes.NFLNK, "NFLNK")]
    [InlineData(NFSItemTypes.NFSOCK, "NFSOCK")]
    public void ToString_IncludesCorrectTypeName(NFSItemTypes type, string expectedTypeName)
    {
        // Arrange
        var attrs = new NFSAttributes(0, 0, 0, type, new NFSPermission(), 0, new byte[4]);

        // Act
        var result = attrs.ToString();

        // Assert
        result.Should().Contain($"Type: {expectedTypeName}");
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var attrs = new NFSAttributes(1000, 2000, 3000, NFSItemTypes.NFREG,
            new NFSPermission(7, 5, 5), 1024, new byte[] { 1, 2, 3, 4 });

        // Assert - Properties should not have setters (compile-time check via usage)
        attrs.CreateDateTime.Should().NotBe(default);
        attrs.LastAccessedDateTime.Should().NotBe(default);
        attrs.ModifiedDateTime.Should().NotBe(default);
        attrs.NFSType.Should().Be(NFSItemTypes.NFREG);
        attrs.Mode.Should().NotBeNull();
        attrs.Size.Should().Be(1024);
        attrs.Handle.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void NFSAttributes_TypicalFileScenario()
    {
        // Arrange - Simulate a typical file with realistic values
        int createTime = 1700000000;  // Nov 14, 2023
        int accessTime = 1700086400;  // Nov 15, 2023
        int modifyTime = 1700043200;  // Nov 14, 2023 (later)
        var type = NFSItemTypes.NFREG;
        var mode = new NFSPermission(6, 4, 4); // rw-r--r-- (644)
        long size = 4096;
        var handle = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        // Act
        var attrs = new NFSAttributes(createTime, accessTime, modifyTime, type, mode, size, handle);

        // Assert
        attrs.NFSType.Should().Be(NFSItemTypes.NFREG);
        attrs.Size.Should().Be(4096);
        attrs.Mode.UserAccess.Should().Be(6);
        attrs.Mode.GroupAccess.Should().Be(4);
        attrs.Mode.OtherAccess.Should().Be(4);
        attrs.Handle.Should().HaveCount(8);
        attrs.CreateDateTime.Should().BeBefore(attrs.ModifiedDateTime);
        attrs.ModifiedDateTime.Should().BeBefore(attrs.LastAccessedDateTime);
    }

    [Fact]
    public void NFSAttributes_TypicalDirectoryScenario()
    {
        // Arrange - Simulate a typical directory
        int timestamp = 1700000000;
        var type = NFSItemTypes.NFDIR;
        var mode = new NFSPermission(7, 5, 5); // rwxr-xr-x (755)
        long size = 4096; // Typical directory size

        // Act
        var attrs = new NFSAttributes(timestamp, timestamp, timestamp, type, mode, size, new byte[32]);

        // Assert
        attrs.NFSType.Should().Be(NFSItemTypes.NFDIR);
        attrs.Mode.UserAccess.Should().Be(7);
        attrs.Mode.Mode.Should().Be(0x1ED); // 755 octal = 493 decimal
    }

    [Fact]
    public void NFSAttributes_SymbolicLinkScenario()
    {
        // Arrange
        var attrs = new NFSAttributes(1700000000, 1700000000, 1700000000,
            NFSItemTypes.NFLNK,
            new NFSPermission(7, 7, 7), // Symlinks typically have 777
            24, // Link target path length
            new byte[16]);

        // Assert
        attrs.NFSType.Should().Be(NFSItemTypes.NFLNK);
        attrs.Mode.UserAccess.Should().Be(7);
    }

    #endregion
}
