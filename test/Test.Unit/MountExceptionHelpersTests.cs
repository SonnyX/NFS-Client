using FluentAssertions;
using NFSLibrary.Protocols.Commons;
using NFSLibrary.Protocols.Commons.Exceptions.Mount;

namespace Test.Unit;

/// <summary>
/// Unit tests for the MountExceptionHelpers class.
/// Tests that mount error codes are correctly mapped to appropriate exception types.
/// </summary>
public class MountExceptionHelpersTests
{
    #region MNT_OK Tests

    [Fact]
    public void ThrowException_MNT_OK_DoesNotThrow()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNT_OK);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region NFSMountAuthenticationException Tests

    [Fact]
    public void ThrowException_MNTERR_ACCES_ThrowsNFSMountAuthenticationException()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_ACCES);

        // Assert
        act.Should().Throw<NFSMountAuthenticationException>()
            .WithMessage("*Permission denied*");
    }

    #endregion

    #region NFSMountUnauthorizedAccessException Tests

    [Theory]
    [InlineData(NFSMountStats.MNTERR_PERM)]
    [InlineData(NFSMountStats.MNTERR_ROFS)]
    public void ThrowException_PermissionErrors_ThrowNFSMountUnauthorizedAccessException(NFSMountStats errorCode)
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(errorCode);

        // Assert
        act.Should().Throw<NFSMountUnauthorizedAccessException>();
    }

    [Fact]
    public void ThrowException_MNTERR_PERM_ContainsNotOwnerMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_PERM);

        // Assert
        act.Should().Throw<NFSMountUnauthorizedAccessException>()
            .WithMessage("*Not owner*");
    }

    [Fact]
    public void ThrowException_MNTERR_ROFS_ContainsReadOnlyMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_ROFS);

        // Assert
        act.Should().Throw<NFSMountUnauthorizedAccessException>()
            .WithMessage("*Read-only file system*");
    }

    #endregion

    #region NFSMountIOException Tests

    [Theory]
    [InlineData(NFSMountStats.MNTERR_EXIST)]
    [InlineData(NFSMountStats.MNTERR_FBIG)]
    [InlineData(NFSMountStats.MNTERR_IO)]
    [InlineData(NFSMountStats.MNTERR_ISDIR)]
    [InlineData(NFSMountStats.MNTERR_NAMETOOLONG)]
    [InlineData(NFSMountStats.MNTERR_NOENT)]
    [InlineData(NFSMountStats.MNTERR_NOSPC)]
    [InlineData(NFSMountStats.MNTERR_NOTDIR)]
    [InlineData(NFSMountStats.MNTERR_NXIO)]
    public void ThrowException_IOErrors_ThrowNFSMountIOException(NFSMountStats errorCode)
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(errorCode);

        // Assert
        act.Should().Throw<NFSMountIOException>();
    }

    [Fact]
    public void ThrowException_MNTERR_NOENT_ContainsNoSuchFileMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_NOENT);

        // Assert
        act.Should().Throw<NFSMountIOException>()
            .WithMessage("*No such file or directory*");
    }

    [Fact]
    public void ThrowException_MNTERR_IO_ContainsIOErrorMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_IO);

        // Assert
        act.Should().Throw<NFSMountIOException>()
            .WithMessage("*I/O error*");
    }

    [Fact]
    public void ThrowException_MNTERR_NOSPC_ContainsNoSpaceMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_NOSPC);

        // Assert
        act.Should().Throw<NFSMountIOException>()
            .WithMessage("*No space left on device*");
    }

    [Fact]
    public void ThrowException_MNTERR_EXIST_ContainsFileExistsMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_EXIST);

        // Assert
        act.Should().Throw<NFSMountIOException>()
            .WithMessage("*File exists*");
    }

    [Fact]
    public void ThrowException_MNTERR_NOTDIR_ContainsNotDirectoryMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_NOTDIR);

        // Assert
        act.Should().Throw<NFSMountIOException>()
            .WithMessage("*Not a directory*");
    }

    [Fact]
    public void ThrowException_MNTERR_ISDIR_ContainsIsDirectoryMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_ISDIR);

        // Assert
        act.Should().Throw<NFSMountIOException>()
            .WithMessage("*Is a directory*");
    }

    [Fact]
    public void ThrowException_MNTERR_FBIG_ContainsFileTooLargeMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_FBIG);

        // Assert
        act.Should().Throw<NFSMountIOException>()
            .WithMessage("*File too large*");
    }

    #endregion

    #region NFSMountCommunicationException Tests

    [Theory]
    [InlineData(NFSMountStats.MNTERR_FAULT)]
    [InlineData(NFSMountStats.MNTERR_BUSY)]
    [InlineData(NFSMountStats.MNTERR_AGAIN)]
    [InlineData(NFSMountStats.MNTERR_SERVERFAULT)]
    public void ThrowException_CommunicationErrors_ThrowNFSMountCommunicationException(NFSMountStats errorCode)
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(errorCode);

        // Assert
        act.Should().Throw<NFSMountCommunicationException>();
    }

    [Fact]
    public void ThrowException_MNTERR_FAULT_ContainsBadAddressMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_FAULT);

        // Assert
        act.Should().Throw<NFSMountCommunicationException>()
            .WithMessage("*Bad address*");
    }

    [Fact]
    public void ThrowException_MNTERR_BUSY_ContainsBusyMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_BUSY);

        // Assert
        act.Should().Throw<NFSMountCommunicationException>()
            .WithMessage("*busy*");
    }

    [Fact]
    public void ThrowException_MNTERR_SERVERFAULT_ContainsServerFailureMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_SERVERFAULT);

        // Assert
        act.Should().Throw<NFSMountCommunicationException>()
            .WithMessage("*failure on the server*");
    }

    #endregion

    #region NFSMountGeneralException Tests

    [Theory]
    [InlineData(NFSMountStats.MNTERR_TXTBSY)]
    [InlineData(NFSMountStats.MNTERR_TOOBIG)]
    [InlineData(NFSMountStats.MNTERR_SRCH)]
    [InlineData(NFSMountStats.MNTERR_NOTTY)]
    [InlineData(NFSMountStats.MNTERR_NOTBLK)]
    [InlineData(NFSMountStats.MNTERR_NOMEM)]
    [InlineData(NFSMountStats.MNTERR_NOEXEC)]
    [InlineData(NFSMountStats.MNTERR_INVAL)]
    [InlineData(NFSMountStats.MNTERR_INTR)]
    [InlineData(NFSMountStats.MNTERR_CHILD)]
    [InlineData(NFSMountStats.MNTERR_BADF)]
    [InlineData(NFSMountStats.MNTERR_XDEV)]
    [InlineData(NFSMountStats.MNTERR_NOTSUPP)]
    [InlineData(NFSMountStats.MNTERR_NODEV)]
    [InlineData(NFSMountStats.MNTERR_MLINK)]
    [InlineData(NFSMountStats.MNTERR_MFILE)]
    [InlineData(NFSMountStats.MNTERR_NFILE)]
    [InlineData(NFSMountStats.MNTERR_PIPE)]
    [InlineData(NFSMountStats.MNTERR_SPIPE)]
    public void ThrowException_GeneralErrors_ThrowNFSMountGeneralException(NFSMountStats errorCode)
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(errorCode);

        // Assert
        act.Should().Throw<NFSMountGeneralException>();
    }

    [Fact]
    public void ThrowException_MNTERR_INVAL_ContainsInvalidArgumentMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_INVAL);

        // Assert
        act.Should().Throw<NFSMountGeneralException>()
            .WithMessage("*Invalid argument*");
    }

    [Fact]
    public void ThrowException_MNTERR_NOMEM_ContainsOutOfMemoryMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_NOMEM);

        // Assert
        act.Should().Throw<NFSMountGeneralException>()
            .WithMessage("*memory*");
    }

    [Fact]
    public void ThrowException_MNTERR_NOTSUPP_ContainsNotSupportedMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_NOTSUPP);

        // Assert
        act.Should().Throw<NFSMountGeneralException>()
            .WithMessage("*not supported*");
    }

    [Fact]
    public void ThrowException_MNTERR_XDEV_ContainsCrossDeviceMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_XDEV);

        // Assert
        act.Should().Throw<NFSMountGeneralException>()
            .WithMessage("*Cross-device*");
    }

    [Fact]
    public void ThrowException_MNTERR_PIPE_ContainsBrokenPipeMessage()
    {
        // Arrange & Act
        var act = () => MountExceptionHelpers.ThrowException(NFSMountStats.MNTERR_PIPE);

        // Assert
        act.Should().Throw<NFSMountGeneralException>()
            .WithMessage("*Broken pipe*");
    }

    #endregion

    #region Unknown Error Code Tests

    [Fact]
    public void ThrowException_UnknownErrorCode_ThrowsNFSMountGeneralException()
    {
        // Arrange - use an undefined error code
        var unknownCode = (NFSMountStats)99999;

        // Act
        var act = () => MountExceptionHelpers.ThrowException(unknownCode);

        // Assert
        act.Should().Throw<NFSMountGeneralException>()
            .WithMessage("*Unknown*");
    }

    #endregion

    #region All Error Codes Have Mappings Tests

    [Fact]
    public void ThrowException_AllDefinedErrorCodes_ThrowSomeException()
    {
        // Arrange - get all defined NFSMountStats values except MNT_OK
        var allErrorCodes = Enum.GetValues<NFSMountStats>()
            .Where(code => code != NFSMountStats.MNT_OK);

        // Act & Assert - each should throw some exception
        foreach (var errorCode in allErrorCodes)
        {
            var act = () => MountExceptionHelpers.ThrowException(errorCode);
            act.Should().Throw<Exception>($"Error code {errorCode} should throw an exception");
        }
    }

    #endregion
}
