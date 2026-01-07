using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NFSLibrary;
using NFSLibrary.DependencyInjection;

namespace Test.Unit;

/// <summary>
/// Unit tests for DependencyInjection components including ServiceCollectionExtensions,
/// NfsClientOptions, NfsClientOptionsCollection, and INfsClientFactory.
/// </summary>
public class DependencyInjectionTests
{
    #region NfsClientOptions Tests

    [Fact]
    public void NfsClientOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new NfsClientOptions();

        // Assert
        options.Name.Should().BeNull();
        options.ServerAddress.Should().BeNull();
        options.Version.Should().Be(NfsVersion.V3);
        options.UserId.Should().Be(0);
        options.GroupId.Should().Be(0);
        options.TimeoutMs.Should().Be(60000);
        options.UseSecurePort.Should().BeFalse();
        options.UseFhCache.Should().BeTrue();
        options.DefaultExport.Should().BeNull();
    }

    [Fact]
    public void NfsClientOptions_Name_CanBeSet()
    {
        // Arrange
        var options = new NfsClientOptions();

        // Act
        options.Name = "TestClient";

        // Assert
        options.Name.Should().Be("TestClient");
    }

    [Fact]
    public void NfsClientOptions_ServerAddress_CanBeSet()
    {
        // Arrange
        var options = new NfsClientOptions();

        // Act
        options.ServerAddress = "192.168.1.100";

        // Assert
        options.ServerAddress.Should().Be("192.168.1.100");
    }

    [Theory]
    [InlineData(NfsVersion.V2)]
    [InlineData(NfsVersion.V3)]
    [InlineData(NfsVersion.V4)]
    public void NfsClientOptions_Version_AcceptsAllVersions(NfsVersion version)
    {
        // Arrange & Act
        var options = new NfsClientOptions { Version = version };

        // Assert
        options.Version.Should().Be(version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(65534)]
    public void NfsClientOptions_UserId_AcceptsVariousValues(int userId)
    {
        // Arrange & Act
        var options = new NfsClientOptions { UserId = userId };

        // Assert
        options.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(65534)]
    public void NfsClientOptions_GroupId_AcceptsVariousValues(int groupId)
    {
        // Arrange & Act
        var options = new NfsClientOptions { GroupId = groupId };

        // Assert
        options.GroupId.Should().Be(groupId);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(30000)]
    [InlineData(60000)]
    [InlineData(120000)]
    public void NfsClientOptions_TimeoutMs_AcceptsVariousValues(int timeoutMs)
    {
        // Arrange & Act
        var options = new NfsClientOptions { TimeoutMs = timeoutMs };

        // Assert
        options.TimeoutMs.Should().Be(timeoutMs);
    }

    [Fact]
    public void NfsClientOptions_UseSecurePort_CanBeEnabled()
    {
        // Arrange
        var options = new NfsClientOptions();

        // Act
        options.UseSecurePort = true;

        // Assert
        options.UseSecurePort.Should().BeTrue();
    }

    [Fact]
    public void NfsClientOptions_UseFhCache_CanBeDisabled()
    {
        // Arrange
        var options = new NfsClientOptions();

        // Act
        options.UseFhCache = false;

        // Assert
        options.UseFhCache.Should().BeFalse();
    }

    [Fact]
    public void NfsClientOptions_DefaultExport_CanBeSet()
    {
        // Arrange
        var options = new NfsClientOptions();

        // Act
        options.DefaultExport = "/export/data";

        // Assert
        options.DefaultExport.Should().Be("/export/data");
    }

    #endregion

    #region NfsClientOptionsCollection Tests

    [Fact]
    public void NfsClientOptionsCollection_AddOptions_StoresOptions()
    {
        // Arrange
        var collection = new NfsClientOptionsCollection();
        var options = new NfsClientOptions { ServerAddress = "192.168.1.1" };

        // Act
        collection.AddOptions("server1", options);
        var result = collection.GetOptions("server1");

        // Assert
        result.Should().NotBeNull();
        result!.ServerAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public void NfsClientOptionsCollection_GetOptions_NonExistent_ReturnsNull()
    {
        // Arrange
        var collection = new NfsClientOptionsCollection();

        // Act
        var result = collection.GetOptions("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void NfsClientOptionsCollection_AddOptions_SameName_OverwritesExisting()
    {
        // Arrange
        var collection = new NfsClientOptionsCollection();
        var options1 = new NfsClientOptions { ServerAddress = "192.168.1.1" };
        var options2 = new NfsClientOptions { ServerAddress = "192.168.1.2" };

        // Act
        collection.AddOptions("server1", options1);
        collection.AddOptions("server1", options2);
        var result = collection.GetOptions("server1");

        // Assert
        result.Should().NotBeNull();
        result!.ServerAddress.Should().Be("192.168.1.2");
    }

    [Fact]
    public void NfsClientOptionsCollection_MultipleOptions_StoresAll()
    {
        // Arrange
        var collection = new NfsClientOptionsCollection();
        var options1 = new NfsClientOptions { ServerAddress = "192.168.1.1" };
        var options2 = new NfsClientOptions { ServerAddress = "192.168.1.2" };
        var options3 = new NfsClientOptions { ServerAddress = "192.168.1.3" };

        // Act
        collection.AddOptions("server1", options1);
        collection.AddOptions("server2", options2);
        collection.AddOptions("server3", options3);

        // Assert
        collection.GetOptions("server1")!.ServerAddress.Should().Be("192.168.1.1");
        collection.GetOptions("server2")!.ServerAddress.Should().Be("192.168.1.2");
        collection.GetOptions("server3")!.ServerAddress.Should().Be("192.168.1.3");
    }

    #endregion

    #region ServiceCollectionExtensions AddNfsClient Tests

    [Fact]
    public void AddNfsClient_DefaultConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsClient();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsClientOptions));
        services.Should().Contain(sd => sd.ServiceType == typeof(INfsClientFactory));
    }

    [Fact]
    public void AddNfsClient_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsClient(options =>
        {
            options.ServerAddress = "192.168.1.100";
            options.Version = NfsVersion.V4;
            options.UserId = 1000;
            options.GroupId = 1000;
        });

        // Assert - Build provider and verify options
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<NfsClientOptions>();

        options.ServerAddress.Should().Be("192.168.1.100");
        options.Version.Should().Be(NfsVersion.V4);
        options.UserId.Should().Be(1000);
        options.GroupId.Should().Be(1000);
    }

    [Fact]
    public void AddNfsClient_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddNfsClient();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddNfsClient_CalledTwice_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsClient();
        services.AddNfsClient();

        // Assert - TryAdd should prevent duplicates
        services.Count(sd => sd.ServiceType == typeof(NfsClientOptions)).Should().Be(1);
        services.Count(sd => sd.ServiceType == typeof(INfsClientFactory)).Should().Be(1);
    }

    #endregion

    #region ServiceCollectionExtensions AddNfsClient (Named) Tests

    [Fact]
    public void AddNfsClient_Named_RegistersOptionsCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsClient("server1", options =>
        {
            options.ServerAddress = "192.168.1.1";
        });

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsClientOptionsCollection));
    }

    [Fact]
    public void AddNfsClient_Named_StoresOptionsWithName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsClient("server1", options =>
        {
            options.ServerAddress = "192.168.1.1";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var collection = provider.GetRequiredService<NfsClientOptionsCollection>();
        var options = collection.GetOptions("server1");

        options.Should().NotBeNull();
        options!.Name.Should().Be("server1");
        options.ServerAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public void AddNfsClient_Named_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddNfsClient(null!, options => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void AddNfsClient_Named_EmptyName_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddNfsClient("", options => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void AddNfsClient_Named_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddNfsClient("server1", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    [Fact]
    public void AddNfsClient_MultipleNamed_StoresAllConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsClient("server1", options => options.ServerAddress = "192.168.1.1");
        services.AddNfsClient("server2", options => options.ServerAddress = "192.168.1.2");

        // Assert
        var provider = services.BuildServiceProvider();
        var collection = provider.GetRequiredService<NfsClientOptionsCollection>();

        collection.GetOptions("server1")!.ServerAddress.Should().Be("192.168.1.1");
        collection.GetOptions("server2")!.ServerAddress.Should().Be("192.168.1.2");
    }

    #endregion

    #region ServiceCollectionExtensions AddNfsConnectionPool Tests

    [Fact]
    public void AddNfsConnectionPool_DefaultConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsConnectionPool();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsConnectionPoolOptions));
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsConnectionPool));
    }

    [Fact]
    public void AddNfsConnectionPool_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsConnectionPool(options =>
        {
            options.MaxPoolSize = 20;
            options.IdleTimeout = TimeSpan.FromMinutes(10);
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<NfsConnectionPoolOptions>();

        options.MaxPoolSize.Should().Be(20);
        options.IdleTimeout.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void AddNfsConnectionPool_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddNfsConnectionPool();

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion

    #region ServiceCollectionExtensions AddNfsHealthChecks Tests

    [Fact]
    public void AddNfsHealthChecks_DefaultConfiguration_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsHealthChecks();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsConnectionHealthOptions));
    }

    [Fact]
    public void AddNfsHealthChecks_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNfsHealthChecks(options =>
        {
            options.EnableAutoHeartbeat = false;
            options.HeartbeatInterval = TimeSpan.FromMinutes(1);
            options.UnhealthyThreshold = 5;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<NfsConnectionHealthOptions>();

        options.EnableAutoHeartbeat.Should().BeFalse();
        options.HeartbeatInterval.Should().Be(TimeSpan.FromMinutes(1));
        options.UnhealthyThreshold.Should().Be(5);
    }

    [Fact]
    public void AddNfsHealthChecks_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddNfsHealthChecks();

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void ServiceCollectionExtensions_AllMethods_CanBeChained()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should not throw
        services
            .AddNfsClient(options => options.ServerAddress = "192.168.1.1")
            .AddNfsClient("server2", options => options.ServerAddress = "192.168.1.2")
            .AddNfsConnectionPool(options => options.MaxPoolSize = 20)
            .AddNfsHealthChecks(options => options.EnableAutoHeartbeat = false);

        // Verify all services are registered
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsClientOptions));
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsClientOptionsCollection));
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsConnectionPoolOptions));
        services.Should().Contain(sd => sd.ServiceType == typeof(NfsConnectionHealthOptions));
    }

    #endregion

    #region INfsClientFactory Interface Tests

    [Fact]
    public void INfsClientFactory_IsRegistered_CanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNfsClient();

        // Act
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<INfsClientFactory>();

        // Assert
        factory.Should().NotBeNull();
        factory.Should().BeAssignableTo<INfsClientFactory>();
    }

    #endregion
}
