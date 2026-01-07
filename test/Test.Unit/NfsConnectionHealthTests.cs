using FluentAssertions;
using NFSLibrary;

namespace Test.Unit;

/// <summary>
/// Unit tests for NfsConnectionHealth, NfsConnectionHealthOptions, and HealthCheckResult.
/// Note: Tests requiring actual NFS connections are in integration tests.
/// These tests focus on configuration, data structures, and enum values.
/// </summary>
public class NfsConnectionHealthTests
{
    #region NfsConnectionHealthOptions Tests

    [Fact]
    public void NfsConnectionHealthOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new NfsConnectionHealthOptions();

        // Assert
        options.EnableAutoHeartbeat.Should().BeTrue();
        options.HeartbeatInterval.Should().Be(TimeSpan.FromSeconds(30));
        options.UnhealthyThreshold.Should().Be(3);
        options.HealthCheckTimeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void NfsConnectionHealthOptions_EnableAutoHeartbeat_CanBeDisabled()
    {
        // Arrange
        var options = new NfsConnectionHealthOptions();

        // Act
        options.EnableAutoHeartbeat = false;

        // Assert
        options.EnableAutoHeartbeat.Should().BeFalse();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(300)]
    public void NfsConnectionHealthOptions_HeartbeatInterval_AcceptsVariousValues(int seconds)
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(seconds);

        // Act
        var options = new NfsConnectionHealthOptions { HeartbeatInterval = interval };

        // Assert
        options.HeartbeatInterval.Should().Be(interval);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void NfsConnectionHealthOptions_UnhealthyThreshold_AcceptsVariousValues(int threshold)
    {
        // Arrange & Act
        var options = new NfsConnectionHealthOptions { UnhealthyThreshold = threshold };

        // Assert
        options.UnhealthyThreshold.Should().Be(threshold);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(30)]
    public void NfsConnectionHealthOptions_HealthCheckTimeout_AcceptsVariousValues(int seconds)
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(seconds);

        // Act
        var options = new NfsConnectionHealthOptions { HealthCheckTimeout = timeout };

        // Assert
        options.HealthCheckTimeout.Should().Be(timeout);
    }

    #endregion

    #region HealthCheckResult Tests

    [Fact]
    public void HealthCheckResult_HealthyResult_HasCorrectProperties()
    {
        // Arrange
        var latency = TimeSpan.FromMilliseconds(50);
        var message = "Connection healthy";

        // Act
        var result = new HealthCheckResult(
            isHealthy: true,
            latency: latency,
            message: message);

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.Latency.Should().Be(latency);
        result.Message.Should().Be(message);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void HealthCheckResult_UnhealthyResult_HasCorrectProperties()
    {
        // Arrange
        var latency = TimeSpan.FromMilliseconds(5000);
        var message = "Connection failed";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = new HealthCheckResult(
            isHealthy: false,
            latency: latency,
            message: message,
            exception: exception);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Latency.Should().Be(latency);
        result.Message.Should().Be(message);
        result.Exception.Should().Be(exception);
    }

    [Fact]
    public void HealthCheckResult_UnhealthyWithoutException_HasNullException()
    {
        // Arrange & Act
        var result = new HealthCheckResult(
            isHealthy: false,
            latency: TimeSpan.FromMilliseconds(100),
            message: "Timeout occurred");

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Exception.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void HealthCheckResult_VariousLatencies_StoredCorrectly(int milliseconds)
    {
        // Arrange
        var latency = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        var result = new HealthCheckResult(true, latency, "OK");

        // Assert
        result.Latency.Should().Be(latency);
    }

    [Fact]
    public void HealthCheckResult_ZeroLatency_IsValid()
    {
        // Arrange & Act
        var result = new HealthCheckResult(true, TimeSpan.Zero, "Instant check");

        // Assert
        result.Latency.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void HealthCheckResult_EmptyMessage_IsValid()
    {
        // Arrange & Act
        var result = new HealthCheckResult(true, TimeSpan.FromMilliseconds(10), string.Empty);

        // Assert
        result.Message.Should().BeEmpty();
    }

    [Fact]
    public void HealthCheckResult_NullMessage_IsValid()
    {
        // Arrange & Act
        var result = new HealthCheckResult(true, TimeSpan.FromMilliseconds(10), null!);

        // Assert
        result.Message.Should().BeNull();
    }

    #endregion

    #region ConnectionHealthStatus Enum Tests

    [Fact]
    public void ConnectionHealthStatus_HasAllExpectedValues()
    {
        // Assert
        Enum.GetValues<ConnectionHealthStatus>().Should().HaveCount(4);
        Enum.IsDefined(ConnectionHealthStatus.Unknown).Should().BeTrue();
        Enum.IsDefined(ConnectionHealthStatus.Healthy).Should().BeTrue();
        Enum.IsDefined(ConnectionHealthStatus.Degraded).Should().BeTrue();
        Enum.IsDefined(ConnectionHealthStatus.Unhealthy).Should().BeTrue();
    }

    [Fact]
    public void ConnectionHealthStatus_Unknown_IsDefaultValue()
    {
        // Arrange & Act
        var defaultStatus = default(ConnectionHealthStatus);

        // Assert
        defaultStatus.Should().Be(ConnectionHealthStatus.Unknown);
    }

    [Theory]
    [InlineData(ConnectionHealthStatus.Unknown, 0)]
    [InlineData(ConnectionHealthStatus.Healthy, 1)]
    [InlineData(ConnectionHealthStatus.Degraded, 2)]
    [InlineData(ConnectionHealthStatus.Unhealthy, 3)]
    public void ConnectionHealthStatus_HasExpectedNumericValues(ConnectionHealthStatus status, int expectedValue)
    {
        // Assert
        ((int)status).Should().Be(expectedValue);
    }

    #endregion

    #region ConnectionHealthChangedEventArgs Tests

    [Fact]
    public void ConnectionHealthChangedEventArgs_StoresStatusValues()
    {
        // Arrange
        var oldStatus = ConnectionHealthStatus.Unknown;
        var newStatus = ConnectionHealthStatus.Healthy;

        // Act
        var args = new ConnectionHealthChangedEventArgs(oldStatus, newStatus);

        // Assert
        args.OldStatus.Should().Be(oldStatus);
        args.NewStatus.Should().Be(newStatus);
    }

    [Theory]
    [InlineData(ConnectionHealthStatus.Unknown, ConnectionHealthStatus.Healthy)]
    [InlineData(ConnectionHealthStatus.Healthy, ConnectionHealthStatus.Degraded)]
    [InlineData(ConnectionHealthStatus.Degraded, ConnectionHealthStatus.Unhealthy)]
    [InlineData(ConnectionHealthStatus.Unhealthy, ConnectionHealthStatus.Healthy)]
    public void ConnectionHealthChangedEventArgs_VariousTransitions_StoredCorrectly(
        ConnectionHealthStatus oldStatus, ConnectionHealthStatus newStatus)
    {
        // Arrange & Act
        var args = new ConnectionHealthChangedEventArgs(oldStatus, newStatus);

        // Assert
        args.OldStatus.Should().Be(oldStatus);
        args.NewStatus.Should().Be(newStatus);
    }

    [Fact]
    public void ConnectionHealthChangedEventArgs_SameOldAndNewStatus_IsValid()
    {
        // Arrange & Act
        var args = new ConnectionHealthChangedEventArgs(
            ConnectionHealthStatus.Healthy,
            ConnectionHealthStatus.Healthy);

        // Assert - Same status transition should be allowed
        args.OldStatus.Should().Be(ConnectionHealthStatus.Healthy);
        args.NewStatus.Should().Be(ConnectionHealthStatus.Healthy);
    }

    [Fact]
    public void ConnectionHealthChangedEventArgs_InheritsFromEventArgs()
    {
        // Arrange & Act
        var args = new ConnectionHealthChangedEventArgs(
            ConnectionHealthStatus.Unknown,
            ConnectionHealthStatus.Healthy);

        // Assert
        args.Should().BeAssignableTo<EventArgs>();
    }

    #endregion

    #region NfsConnectionHealth Constructor Tests

    [Fact]
    public void NfsConnectionHealth_NullClient_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new NfsConnectionHealth(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("client");
    }

    #endregion
}
