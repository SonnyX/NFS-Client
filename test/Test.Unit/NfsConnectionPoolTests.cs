using FluentAssertions;
using NFSLibrary;

namespace Test.Unit;

/// <summary>
/// Unit tests for NfsConnectionPool and NfsConnectionPoolOptions.
/// Note: Many pool operations require actual NFS connections and are better suited
/// for integration tests. These tests focus on configuration and lifecycle.
/// </summary>
public class NfsConnectionPoolTests
{
    #region NfsConnectionPoolOptions Tests

    [Fact]
    public void NfsConnectionPoolOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new NfsConnectionPoolOptions();

        // Assert
        options.MaxPoolSize.Should().Be(10);
        options.IdleTimeout.Should().Be(TimeSpan.FromMinutes(5));
        options.EnableMaintenance.Should().BeTrue();
        options.MaintenanceInterval.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void NfsConnectionPoolOptions_MaxPoolSize_CanBeSet()
    {
        // Arrange
        var options = new NfsConnectionPoolOptions();

        // Act
        options.MaxPoolSize = 25;

        // Assert
        options.MaxPoolSize.Should().Be(25);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    [InlineData(1000)]
    public void NfsConnectionPoolOptions_MaxPoolSize_AcceptsVariousValues(int maxSize)
    {
        // Arrange & Act
        var options = new NfsConnectionPoolOptions { MaxPoolSize = maxSize };

        // Assert
        options.MaxPoolSize.Should().Be(maxSize);
    }

    [Fact]
    public void NfsConnectionPoolOptions_IdleTimeout_CanBeSet()
    {
        // Arrange
        var options = new NfsConnectionPoolOptions();

        // Act
        options.IdleTimeout = TimeSpan.FromMinutes(15);

        // Assert
        options.IdleTimeout.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Theory]
    [InlineData(30)]      // 30 seconds
    [InlineData(300)]     // 5 minutes
    [InlineData(3600)]    // 1 hour
    public void NfsConnectionPoolOptions_IdleTimeout_AcceptsVariousValues(int seconds)
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(seconds);

        // Act
        var options = new NfsConnectionPoolOptions { IdleTimeout = timeout };

        // Assert
        options.IdleTimeout.Should().Be(timeout);
    }

    [Fact]
    public void NfsConnectionPoolOptions_EnableMaintenance_CanBeDisabled()
    {
        // Arrange
        var options = new NfsConnectionPoolOptions();

        // Act
        options.EnableMaintenance = false;

        // Assert
        options.EnableMaintenance.Should().BeFalse();
    }

    [Fact]
    public void NfsConnectionPoolOptions_MaintenanceInterval_CanBeSet()
    {
        // Arrange
        var options = new NfsConnectionPoolOptions();

        // Act
        options.MaintenanceInterval = TimeSpan.FromSeconds(30);

        // Assert
        options.MaintenanceInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region NfsConnectionPool Constructor Tests

    [Fact]
    public void NfsConnectionPool_DefaultConstructor_CreatesEmptyPool()
    {
        // Arrange & Act
        using var pool = new NfsConnectionPool();

        // Assert
        pool.TotalConnections.Should().Be(0);
        pool.AvailableConnections.Should().Be(0);
    }

    [Fact]
    public void NfsConnectionPool_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var options = new NfsConnectionPoolOptions
        {
            MaxPoolSize = 5,
            EnableMaintenance = false
        };

        // Act
        using var pool = new NfsConnectionPool(options);

        // Assert
        pool.TotalConnections.Should().Be(0);
    }

    [Fact]
    public void NfsConnectionPool_WithNullOptions_UsesDefaults()
    {
        // Arrange & Act
        using var pool = new NfsConnectionPool(null);

        // Assert
        pool.TotalConnections.Should().Be(0);
        pool.AvailableConnections.Should().Be(0);
    }

    #endregion

    #region NfsConnectionPool Lifecycle Tests

    [Fact]
    public void NfsConnectionPool_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var pool = new NfsConnectionPool();

        // Act & Assert - should not throw
        pool.Dispose();
        pool.Dispose();
    }

    [Fact]
    public async Task NfsConnectionPool_DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var pool = new NfsConnectionPool();

        // Act & Assert - should not throw
        await pool.DisposeAsync();
        await pool.DisposeAsync();
    }

    [Fact]
    public void NfsConnectionPool_Clear_EmptiesThePool()
    {
        // Arrange
        using var pool = new NfsConnectionPool();

        // Act
        pool.Clear();

        // Assert
        pool.TotalConnections.Should().Be(0);
        pool.AvailableConnections.Should().Be(0);
    }

    [Fact]
    public void NfsConnectionPool_AfterDispose_GetConnectionThrows()
    {
        // Arrange
        var pool = new NfsConnectionPool();
        pool.Dispose();

        // Act
        var act = () => pool.GetConnection(
            System.Net.IPAddress.Loopback,
            "/export/test",
            NfsVersion.V3);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task NfsConnectionPool_AfterDisposeAsync_GetConnectionAsyncThrows()
    {
        // Arrange
        var pool = new NfsConnectionPool();
        await pool.DisposeAsync();

        // Act
        var act = async () => await pool.GetConnectionAsync(
            System.Net.IPAddress.Loopback,
            "/export/test",
            NfsVersion.V3);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region NfsConnectionPool Statistics Tests

    [Fact]
    public void NfsConnectionPool_TotalConnections_InitiallyZero()
    {
        // Arrange & Act
        using var pool = new NfsConnectionPool();

        // Assert
        pool.TotalConnections.Should().Be(0);
    }

    [Fact]
    public void NfsConnectionPool_AvailableConnections_InitiallyZero()
    {
        // Arrange & Act
        using var pool = new NfsConnectionPool();

        // Assert
        pool.AvailableConnections.Should().Be(0);
    }

    #endregion

    #region NfsConnectionPool with Disabled Maintenance Tests

    [Fact]
    public void NfsConnectionPool_MaintenanceDisabled_DoesNotThrow()
    {
        // Arrange
        var options = new NfsConnectionPoolOptions
        {
            EnableMaintenance = false
        };

        // Act
        using var pool = new NfsConnectionPool(options);

        // Assert
        pool.Should().NotBeNull();
    }

    [Fact]
    public void NfsConnectionPool_MaintenanceEnabled_DoesNotThrow()
    {
        // Arrange
        var options = new NfsConnectionPoolOptions
        {
            EnableMaintenance = true,
            MaintenanceInterval = TimeSpan.FromMilliseconds(100)
        };

        // Act
        using var pool = new NfsConnectionPool(options);

        // Assert
        pool.Should().NotBeNull();
    }

    #endregion
}
