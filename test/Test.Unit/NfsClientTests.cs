using FluentAssertions;
using NFSLibrary;
using NFSLibrary.Protocols.Commons;
using System;
using System.Text;

namespace Test.Unit;

public class NfsClientTests
{
    #region CorrectPath Tests

    [Fact]
    public void CorrectPath_NullInput_ReturnsNull()
    {
        // Arrange & Act
        var result = NfsClient.CorrectPath(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CorrectPath_EmptyInput_ReturnsEmpty()
    {
        // Arrange & Act
        var result = NfsClient.CorrectPath(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CorrectPath_RootPath_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var result = NfsClient.CorrectPath(".");

        // Assert
        result.Should().Be(".");
    }

    [Fact]
    public void CorrectPath_SimpleDirectory_PrependsDotBackslash()
    {
        // Arrange & Act
        var result = NfsClient.CorrectPath("folder");

        // Assert
        result.Should().Be(".\\folder");
    }

    [Fact]
    public void CorrectPath_NestedPath_PreservesStructure()
    {
        // Arrange & Act
        var result = NfsClient.CorrectPath("folder\\subfolder\\file.txt");

        // Assert
        result.Should().Be(".\\folder\\subfolder\\file.txt");
    }

    [Fact]
    public void CorrectPath_AlreadyCorrectPath_ReturnsUnchanged()
    {
        // Arrange & Act
        var result = NfsClient.CorrectPath(".\\folder\\file.txt");

        // Assert
        result.Should().Be(".\\folder\\file.txt");
    }

    [Fact]
    public void CorrectPath_MultipleBackslashes_RemovesEmpty()
    {
        // Arrange & Act
        var result = NfsClient.CorrectPath("folder\\\\subfolder");

        // Assert
        result.Should().Be(".\\folder\\subfolder");
    }

    [Fact]
    public void CorrectPath_LeadingBackslash_HandlesCorrectly()
    {
        // Arrange & Act
        var result = NfsClient.CorrectPath("\\folder\\file.txt");

        // Assert
        result.Should().Be(".\\folder\\file.txt");
    }

    #endregion

    #region Constructor Tests

    [Theory]
    [InlineData(NfsVersion.V2)]
    [InlineData(NfsVersion.V3)]
    [InlineData(NfsVersion.V4)]
    public void Constructor_ValidVersion_CreatesInstance(NfsVersion version)
    {
        // Arrange & Act
        using var client = new NfsClient(version);

        // Assert
        client.Should().NotBeNull();
        client.IsMounted.Should().BeFalse();
        client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_DefaultBlockSize_IsSet()
    {
        // Arrange & Act
        using var client = new NfsClient(NfsVersion.V3);

        // Assert
        client.BlockSize.Should().Be(7900);
    }

    #endregion

    #region Mode Property Tests

    [Fact]
    public void Mode_DefaultValue_Returns777()
    {
        // Arrange
        using var client = new NfsClient(NfsVersion.V3);

        // Act
        var mode = client.Mode;

        // Assert
        mode.Should().NotBeNull();
        mode.UserAccess.Should().Be(7);
        mode.GroupAccess.Should().Be(7);
        mode.OtherAccess.Should().Be(7);
    }

    [Fact]
    public void Mode_SetValue_RetainsValue()
    {
        // Arrange
        using var client = new NfsClient(NfsVersion.V3);
        var newMode = new NFSPermission(6, 4, 0);

        // Act
        client.Mode = newMode;

        // Assert
        client.Mode.UserAccess.Should().Be((byte)6);
        client.Mode.GroupAccess.Should().Be((byte)4);
        client.Mode.OtherAccess.Should().Be((byte)0);
    }

    #endregion

    #region IDisposable Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var client = new NfsClient(NfsVersion.V3);

        // Act & Assert - should not throw
        client.Dispose();
        client.Dispose();
    }

    [Fact]
    public void UsingStatement_DisposesCorrectly()
    {
        // Arrange & Act & Assert - should not throw
        using (var client = new NfsClient(NfsVersion.V3))
        {
            client.Should().NotBeNull();
        }
    }

    #endregion

    #region NfsConnectionOptions Tests

    [Fact]
    public void NfsConnectionOptions_Default_HasExpectedValues()
    {
        // Arrange & Act
        var options = NfsConnectionOptions.Default;

        // Assert
        options.UserId.Should().Be(0);
        options.GroupId.Should().Be(0);
        options.CommandTimeoutMs.Should().Be(60000);
        options.CharacterEncoding.Should().Be(Encoding.ASCII);
        options.UseSecurePort.Should().BeTrue();
        options.UseFileHandleCache.Should().BeFalse();
    }

    [Fact]
    public void NfsConnectionOptions_CommandTimeout_SetCorrectly()
    {
        // Arrange
        var options = new NfsConnectionOptions();

        // Act
        options.CommandTimeout = TimeSpan.FromMinutes(2);

        // Assert
        options.CommandTimeoutMs.Should().Be(120000);
        options.CommandTimeout.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void NfsConnectionOptions_ForUser_CreatesCorrectOptions()
    {
        // Arrange & Act
        var options = NfsConnectionOptions.ForUser(1000, 1000);

        // Assert
        options.UserId.Should().Be(1000);
        options.GroupId.Should().Be(1000);
        options.UseSecurePort.Should().BeTrue(); // defaults preserved
    }

    [Fact]
    public void NfsConnectionOptions_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new NfsConnectionOptions
        {
            UserId = 1000,
            GroupId = 1000,
            CommandTimeoutMs = 30000,
            UseFileHandleCache = true
        };

        // Act
        var clone = original.Clone();
        clone.UserId = 2000;

        // Assert
        original.UserId.Should().Be(1000);
        clone.UserId.Should().Be(2000);
        clone.GroupId.Should().Be(1000);
        clone.CommandTimeoutMs.Should().Be(30000);
        clone.UseFileHandleCache.Should().BeTrue();
    }

    #endregion
}
