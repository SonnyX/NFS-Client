using FluentAssertions;
using NFSLibrary.Protocols.Commons;

namespace Test.Unit;

public class NFSPermissionTests
{
    [Theory]
    [InlineData(7, 7, 7)]
    [InlineData(6, 4, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(4, 2, 1)]
    public void Constructor_ValidValues_StoresCorrectly(byte user, byte group, byte other)
    {
        // Act
        var permission = new NFSPermission(user, group, other);

        // Assert
        permission.UserAccess.Should().Be(user);
        permission.GroupAccess.Should().Be(group);
        permission.OtherAccess.Should().Be(other);
    }

    [Fact]
    public void Mode_FullPermissions_Returns511()
    {
        // Arrange
        var permission = new NFSPermission(7, 7, 7);

        // Act
        var mode = permission.Mode;

        // Assert
        mode.Should().Be(0x1FF); // 511 = 0777 in octal
    }

    [Fact]
    public void Mode_ReadWriteOwner_CalculatesCorrectly()
    {
        // Arrange
        var permission = new NFSPermission(6, 4, 0);

        // Act
        var mode = permission.Mode;

        // Assert
        // 6 << 6 = 384, 4 << 3 = 32, 0 = 0
        // 384 + 32 + 0 = 416 = 0640 in octal
        mode.Should().Be(416);
    }

    [Fact]
    public void Mode_SetValue_UpdatesAccessBits()
    {
        // Arrange
        var permission = new NFSPermission();

        // Act
        permission.Mode = 0x1ED; // 0755 in octal = 493

        // Assert
        permission.UserAccess.Should().Be(7);
        permission.GroupAccess.Should().Be(5);
        permission.OtherAccess.Should().Be(5);
    }

    [Fact]
    public void ToString_FullPermissions_ReturnsRwxFormat()
    {
        // Arrange
        var permission = new NFSPermission(7, 5, 4);

        // Act
        var result = permission.ToString();

        // Assert
        result.Should().Be("rwx | rx | r");
    }
}
