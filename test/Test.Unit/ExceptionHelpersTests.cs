using FluentAssertions;
using NFSLibrary.Protocols.Commons;
using NFSLibrary.Protocols.Commons.Exceptions;

namespace Test.Unit;

/// <summary>
/// Unit tests for the ExceptionHelpers class.
/// Tests that NFS error codes are correctly mapped to appropriate exception types.
/// </summary>
public class ExceptionHelpersTests
{
    #region NFS_OK Tests

    [Fact]
    public void ThrowException_NFS_OK_DoesNotThrow()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFS_OK);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region NFSAuthenticationException Tests

    [Fact]
    public void ThrowException_NFSERR_ACCES_ThrowsNFSAuthenticationException()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_ACCES);

        // Assert
        act.Should().Throw<NFSAuthenticationException>()
            .WithMessage("*Permission denied*");
    }

    #endregion

    #region NFSUnauthorizedAccessException Tests

    [Theory]
    [InlineData(NFSStats.NFSERR_PERM)]
    [InlineData(NFSStats.NFSERR_ROFS)]
    public void ThrowException_PermissionErrors_ThrowNFSUnauthorizedAccessException(NFSStats errorCode)
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(errorCode);

        // Assert
        act.Should().Throw<NFSUnauthorizedAccessException>();
    }

    [Fact]
    public void ThrowException_NFSERR_PERM_ContainsNotOwnerMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_PERM);

        // Assert
        act.Should().Throw<NFSUnauthorizedAccessException>()
            .WithMessage("*Not owner*");
    }

    [Fact]
    public void ThrowException_NFSERR_ROFS_ContainsReadOnlyMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_ROFS);

        // Assert
        act.Should().Throw<NFSUnauthorizedAccessException>()
            .WithMessage("*Read-only file system*");
    }

    #endregion

    #region NFSIOException Tests

    [Theory]
    [InlineData(NFSStats.NFSERR_DQUOT)]
    [InlineData(NFSStats.NFSERR_EXIST)]
    [InlineData(NFSStats.NFSERR_FBIG)]
    [InlineData(NFSStats.NFSERR_IO)]
    [InlineData(NFSStats.NFSERR_ISDIR)]
    [InlineData(NFSStats.NFSERR_NAMETOOLONG)]
    [InlineData(NFSStats.NFSERR_NOENT)]
    [InlineData(NFSStats.NFSERR_NOSPC)]
    [InlineData(NFSStats.NFSERR_NOTDIR)]
    [InlineData(NFSStats.NFSERR_NOTEMPTY)]
    [InlineData(NFSStats.NFSERR_NXIO)]
    public void ThrowException_IOErrors_ThrowNFSIOException(NFSStats errorCode)
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(errorCode);

        // Assert
        act.Should().Throw<NFSIOException>();
    }

    [Fact]
    public void ThrowException_NFSERR_NOENT_ContainsNoSuchFileMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_NOENT);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*No such file or directory*");
    }

    [Fact]
    public void ThrowException_NFSERR_IO_ContainsIOErrorMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_IO);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*I/O error*");
    }

    [Fact]
    public void ThrowException_NFSERR_NOSPC_ContainsNoSpaceMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_NOSPC);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*No space left on device*");
    }

    [Fact]
    public void ThrowException_NFSERR_EXIST_ContainsFileExistsMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_EXIST);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*File exists*");
    }

    [Fact]
    public void ThrowException_NFSERR_NOTDIR_ContainsNotDirectoryMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_NOTDIR);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*Not a directory*");
    }

    [Fact]
    public void ThrowException_NFSERR_ISDIR_ContainsIsDirectoryMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_ISDIR);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*Is a directory*");
    }

    [Fact]
    public void ThrowException_NFSERR_NAMETOOLONG_ContainsNameTooLongMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_NAMETOOLONG);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*filename*too long*");
    }

    [Fact]
    public void ThrowException_NFSERR_NOTEMPTY_ContainsNotEmptyMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_NOTEMPTY);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*not empty*");
    }

    [Fact]
    public void ThrowException_NFSERR_FBIG_ContainsFileTooLargeMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_FBIG);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*File too large*");
    }

    [Fact]
    public void ThrowException_NFSERR_DQUOT_ContainsQuotaMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_DQUOT);

        // Assert
        act.Should().Throw<NFSIOException>()
            .WithMessage("*quota*");
    }

    #endregion

    #region NFSCommunicationException Tests

    [Theory]
    [InlineData(NFSStats.NFSERR_BADHANDLE)]
    [InlineData(NFSStats.NFSERR_BADTYPE)]
    [InlineData(NFSStats.NFSERR_TOOSMALL)]
    [InlineData(NFSStats.NFSERR_SERVERFAULT)]
    [InlineData(NFSStats.NFSERR_JUKEBOX)]
    public void ThrowException_CommunicationErrors_ThrowNFSCommunicationException(NFSStats errorCode)
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(errorCode);

        // Assert
        act.Should().Throw<NFSCommunicationException>();
    }

    [Fact]
    public void ThrowException_NFSERR_BADHANDLE_ContainsInvalidHandleMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_BADHANDLE);

        // Assert
        act.Should().Throw<NFSCommunicationException>()
            .WithMessage("*Illegal NFS file handle*");
    }

    [Fact]
    public void ThrowException_NFSERR_SERVERFAULT_ContainsServerErrorMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_SERVERFAULT);

        // Assert
        act.Should().Throw<NFSCommunicationException>()
            .WithMessage("*error occurred on the server*");
    }

    [Fact]
    public void ThrowException_NFSERR_JUKEBOX_ContainsTimingMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_JUKEBOX);

        // Assert
        act.Should().Throw<NFSCommunicationException>()
            .WithMessage("*not able to complete*timely*");
    }

    #endregion

    #region NFSGeneralException Tests

    [Theory]
    [InlineData(NFSStats.NFSERR_XDEV)]
    [InlineData(NFSStats.NFSERR_REMOTE)]
    [InlineData(NFSStats.NFSERR_NOTSUPP)]
    [InlineData(NFSStats.NFSERR_NOT_SYNC)]
    [InlineData(NFSStats.NFSERR_NODEV)]
    [InlineData(NFSStats.NFSERR_MLINK)]
    [InlineData(NFSStats.NFSERR_INVAL)]
    [InlineData(NFSStats.NFSERR_BAD_COOKIE)]
    [InlineData(NFSStats.NFSERR_STALE)]
    [InlineData(NFSStats.NFSERR_WFLUSH)]
    public void ThrowException_GeneralErrors_ThrowNFSGeneralException(NFSStats errorCode)
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(errorCode);

        // Assert
        act.Should().Throw<NFSGeneralException>();
    }

    [Fact]
    public void ThrowException_NFSERR_STALE_ContainsStaleHandleMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_STALE);

        // Assert
        act.Should().Throw<NFSGeneralException>()
            .WithMessage("*Invalid file handle*");
    }

    [Fact]
    public void ThrowException_NFSERR_NOTSUPP_ContainsNotSupportedMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_NOTSUPP);

        // Assert
        act.Should().Throw<NFSGeneralException>()
            .WithMessage("*not supported*");
    }

    [Fact]
    public void ThrowException_NFSERR_INVAL_ContainsInvalidArgumentMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_INVAL);

        // Assert
        act.Should().Throw<NFSGeneralException>()
            .WithMessage("*Invalid argument*");
    }

    [Fact]
    public void ThrowException_NFSERR_XDEV_ContainsCrossDeviceMessage()
    {
        // Arrange & Act
        var act = () => ExceptionHelpers.ThrowException(NFSStats.NFSERR_XDEV);

        // Assert
        act.Should().Throw<NFSGeneralException>()
            .WithMessage("*cross-device*");
    }

    #endregion

    #region Unknown Error Code Tests

    [Fact]
    public void ThrowException_UnknownErrorCode_ThrowsNFSGeneralException()
    {
        // Arrange - use an undefined error code
        var unknownCode = (NFSStats)99999;

        // Act
        var act = () => ExceptionHelpers.ThrowException(unknownCode);

        // Assert
        act.Should().Throw<NFSGeneralException>()
            .WithMessage("*Unknown*");
    }

    #endregion

    #region All Error Codes Have Mappings Tests

    [Fact]
    public void ThrowException_AllDefinedErrorCodes_ThrowSomeException()
    {
        // Arrange - get all defined NFSStats values except NFS_OK
        var allErrorCodes = Enum.GetValues<NFSStats>()
            .Where(code => code != NFSStats.NFS_OK);

        // Act & Assert - each should throw some exception
        foreach (var errorCode in allErrorCodes)
        {
            var act = () => ExceptionHelpers.ThrowException(errorCode);
            act.Should().Throw<Exception>($"Error code {errorCode} should throw an exception");
        }
    }

    #endregion
}
