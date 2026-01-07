using FluentAssertions;
using NFSLibrary.Protocols.Commons;

namespace Test.Unit;

/// <summary>
/// Unit tests for the Result and Result{T} types.
/// </summary>
public class ResultTests
{
    #region Result<T> Tests

    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Status.Should().Be(NFSStats.NFS_OK);
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        // Arrange & Act
        var result = Result<int>.Failure(NFSStats.NFSERR_NOENT, "File not found");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Status.Should().Be(NFSStats.NFSERR_NOENT);
        result.ErrorMessage.Should().Be("File not found");
    }

    [Fact]
    public void Value_OnFailedResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var result = Result<int>.Failure(NFSStats.NFSERR_NOENT);

        // Act & Assert
        var act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetValueOrDefault_OnSuccess_ShouldReturnValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act & Assert
        result.GetValueOrDefault(0).Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_ShouldReturnDefault()
    {
        // Arrange
        var result = Result<int>.Failure(NFSStats.NFSERR_NOENT);

        // Act & Assert
        result.GetValueOrDefault(99).Should().Be(99);
    }

    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public void Map_OnFailure_ShouldPropagateFailure()
    {
        // Arrange
        var result = Result<int>.Failure(NFSStats.NFSERR_NOENT, "Not found");

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapped.Status.Should().Be(NFSStats.NFSERR_NOENT);
        mapped.ErrorMessage.Should().Be("Not found");
    }

    [Fact]
    public void Bind_OnSuccess_ShouldChainOperations()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var bound = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("42");
    }

    [Fact]
    public void Bind_OnFailure_ShouldNotExecuteChain()
    {
        // Arrange
        var result = Result<int>.Failure(NFSStats.NFSERR_NOENT);
        var chainExecuted = false;

        // Act
        var bound = result.Bind(x =>
        {
            chainExecuted = true;
            return Result<string>.Success(x.ToString());
        });

        // Assert
        bound.IsFailure.Should().BeTrue();
        chainExecuted.Should().BeFalse();
    }

    [Fact]
    public void Match_OnSuccess_ShouldExecuteSuccessFunc()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var matched = result.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: (s, m) => $"Failure: {m}");

        // Assert
        matched.Should().Be("Success: 42");
    }

    [Fact]
    public void Match_OnFailure_ShouldExecuteFailureFunc()
    {
        // Arrange
        var result = Result<int>.Failure(NFSStats.NFSERR_NOENT, "Not found");

        // Act
        var matched = result.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: (s, m) => $"Failure: {m}");

        // Assert
        matched.Should().Be("Failure: Not found");
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        Result<int> result = 42;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void OnSuccess_ShouldExecuteAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executed = false;

        // Act
        result.OnSuccess(x => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void OnFailure_ShouldExecuteAction()
    {
        // Arrange
        var result = Result<int>.Failure(NFSStats.NFSERR_NOENT);
        var executed = false;

        // Act
        result.OnFailure((s, m) => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    #endregion

    #region Non-generic Result Tests

    [Fact]
    public void NonGeneric_Success_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Status.Should().Be(NFSStats.NFS_OK);
    }

    [Fact]
    public void NonGeneric_Failure_ShouldCreateFailedResult()
    {
        // Arrange & Act
        var result = Result.Failure(NFSStats.NFSERR_ACCES, "Permission denied");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Status.Should().Be(NFSStats.NFSERR_ACCES);
        result.ErrorMessage.Should().Be("Permission denied");
    }

    [Fact]
    public void NonGeneric_Match_ShouldExecuteCorrectFunc()
    {
        // Arrange
        var successResult = Result.Success();
        var failureResult = Result.Failure(NFSStats.NFSERR_NOENT);

        // Act & Assert
        successResult.Match(() => "ok", (s, m) => "fail").Should().Be("ok");
        failureResult.Match(() => "ok", (s, m) => "fail").Should().Be("fail");
    }

    #endregion
}
