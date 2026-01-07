using FluentAssertions;
using NFSLibrary.Protocols.Commons;

namespace Test.Unit;

/// <summary>
/// Unit tests for the FileHandleCache class.
/// </summary>
public class FileHandleCacheTests
{
    private static NFSAttributes CreateTestAttributes(byte[] handle)
    {
        return new NFSAttributes(
            cdateTime: 1700000000,
            adateTime: 1700000001,
            mdateTime: 1700000002,
            type: NFSItemTypes.NFREG,
            mode: new NFSPermission(7, 5, 5),
            size: 1024,
            handle: handle);
    }

    private static NFSAttributes CreateTestAttributes(int uniqueId)
    {
        return CreateTestAttributes(new byte[] { (byte)uniqueId, 0, 0, 0 });
    }

    [Fact]
    public void Set_ShouldStoreValue()
    {
        // Arrange
        using var cache = new FileHandleCache();
        var attrs = CreateTestAttributes(new byte[] { 1, 2, 3, 4 });

        // Act
        cache.Set("/test/path", attrs);

        // Assert
        cache.Contains("/test/path").Should().BeTrue();
    }

    [Fact]
    public void Get_ShouldReturnStoredValue()
    {
        // Arrange
        using var cache = new FileHandleCache();
        var handle = new byte[] { 1, 2, 3, 4 };
        var attrs = CreateTestAttributes(handle);
        cache.Set("/test/path", attrs);

        // Act
        var result = cache.Get("/test/path");

        // Assert
        result.Should().NotBeNull();
        result!.Handle.Should().BeEquivalentTo(handle);
        result.Size.Should().Be(1024);
    }

    [Fact]
    public void Get_NonExistentKey_ShouldReturnNull()
    {
        // Arrange
        using var cache = new FileHandleCache();

        // Act
        var result = cache.Get("/nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGet_ExistingKey_ShouldReturnTrue()
    {
        // Arrange
        using var cache = new FileHandleCache();
        var handle = new byte[] { 1, 2, 3, 4 };
        var attrs = CreateTestAttributes(handle);
        cache.Set("/test/path", attrs);

        // Act
        var found = cache.TryGet("/test/path", out var resultHandle, out var resultAttrs);

        // Assert
        found.Should().BeTrue();
        resultHandle.Should().BeEquivalentTo(handle);
        resultAttrs.Should().NotBeNull();
        resultAttrs!.Size.Should().Be(1024);
    }

    [Fact]
    public void TryGet_NonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        using var cache = new FileHandleCache();

        // Act
        var found = cache.TryGet("/nonexistent", out var resultHandle, out var resultAttrs);

        // Assert
        found.Should().BeFalse();
        resultHandle.Should().BeNull();
        resultAttrs.Should().BeNull();
    }

    [Fact]
    public void Invalidate_ShouldRemoveEntry()
    {
        // Arrange
        using var cache = new FileHandleCache();
        var attrs = CreateTestAttributes(new byte[] { 1, 2, 3, 4 });
        cache.Set("/test/path", attrs);

        // Act
        cache.Invalidate("/test/path");

        // Assert
        cache.Contains("/test/path").Should().BeFalse();
    }

    [Fact]
    public void InvalidatePrefix_ShouldRemoveMatchingEntries()
    {
        // Arrange
        using var cache = new FileHandleCache();
        cache.Set("/test/path1", CreateTestAttributes(1));
        cache.Set("/test/path2", CreateTestAttributes(2));
        cache.Set("/other/path", CreateTestAttributes(3));

        // Act
        cache.InvalidatePrefix("/test/");

        // Assert
        cache.Contains("/test/path1").Should().BeFalse();
        cache.Contains("/test/path2").Should().BeFalse();
        cache.Contains("/other/path").Should().BeTrue();
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        using var cache = new FileHandleCache();
        cache.Set("/path1", CreateTestAttributes(1));
        cache.Set("/path2", CreateTestAttributes(2));
        cache.Set("/path3", CreateTestAttributes(3));

        // Act
        cache.Clear();

        // Assert
        cache.Contains("/path1").Should().BeFalse();
        cache.Contains("/path2").Should().BeFalse();
        cache.Contains("/path3").Should().BeFalse();
    }

    [Fact]
    public void GetOrAdd_ExistingKey_ShouldReturnExistingValue()
    {
        // Arrange
        using var cache = new FileHandleCache();
        var originalHandle = new byte[] { 1, 2, 3, 4 };
        var originalAttrs = CreateTestAttributes(originalHandle);
        cache.Set("/test/path", originalAttrs);
        var factoryCallCount = 0;

        // Act
        var result = cache.GetOrAdd("/test/path", path =>
        {
            factoryCallCount++;
            return CreateTestAttributes(new byte[] { 9, 9, 9, 9 });
        });

        // Assert
        result.Handle.Should().BeEquivalentTo(originalHandle);
        factoryCallCount.Should().Be(0);
    }

    [Fact]
    public void GetOrAdd_NonExistentKey_ShouldCallFactoryAndStoreValue()
    {
        // Arrange
        using var cache = new FileHandleCache();
        var newHandle = new byte[] { 5, 6, 7, 8 };
        var factoryCallCount = 0;

        // Act
        var result = cache.GetOrAdd("/new/path", path =>
        {
            factoryCallCount++;
            return CreateTestAttributes(newHandle);
        });

        // Assert
        result.Handle.Should().BeEquivalentTo(newHandle);
        factoryCallCount.Should().Be(1);
        cache.Contains("/new/path").Should().BeTrue();
    }

    [Fact]
    public void Touch_ShouldExtendExpiration()
    {
        // Arrange - use very short expiration for testing
        using var cache = new FileHandleCache(TimeSpan.FromMilliseconds(100));
        cache.Set("/test/path", CreateTestAttributes(new byte[] { 1, 2, 3, 4 }));

        // Act - touch before expiration
        Thread.Sleep(50);
        cache.Touch("/test/path");
        Thread.Sleep(80); // Without touch this would expire (100ms total)

        // Assert - should still be present because we touched it
        cache.Contains("/test/path").Should().BeTrue();
    }

    [Fact]
    public async Task ExpiredEntries_ShouldBeCleanedUp()
    {
        // Arrange - use very short expiration for testing
        using var cache = new FileHandleCache(TimeSpan.FromMilliseconds(50));
        cache.Set("/test/path", CreateTestAttributes(new byte[] { 1, 2, 3, 4 }));

        // Act - wait for expiration and cleanup
        await Task.Delay(200);

        // Assert - entry should be gone after expiration
        cache.Contains("/test/path").Should().BeFalse();
    }

    [Fact]
    public void Count_ShouldReturnNumberOfEntries()
    {
        // Arrange
        using var cache = new FileHandleCache();

        // Act & Assert
        cache.Count.Should().Be(0);
        cache.Set("/path1", CreateTestAttributes(1));
        cache.Count.Should().Be(1);
        cache.Set("/path2", CreateTestAttributes(2));
        cache.Count.Should().Be(2);
        cache.Invalidate("/path1");
        cache.Count.Should().Be(1);
    }

    [Fact]
    public void InvalidateContaining_ShouldRemoveMatchingEntries()
    {
        // Arrange
        using var cache = new FileHandleCache();
        cache.Set("/foo/bar/file1", CreateTestAttributes(1));
        cache.Set("/baz/bar/file2", CreateTestAttributes(2));
        cache.Set("/foo/qux/file3", CreateTestAttributes(3));

        // Act
        cache.InvalidateContaining("/bar/");

        // Assert
        cache.Contains("/foo/bar/file1").Should().BeFalse();
        cache.Contains("/baz/bar/file2").Should().BeFalse();
        cache.Contains("/foo/qux/file3").Should().BeTrue();
    }

    [Fact]
    public void SetWithExplicitHandle_ShouldStoreHandle()
    {
        // Arrange
        using var cache = new FileHandleCache();
        var handle = new byte[] { 10, 20, 30, 40 };
        var attrs = CreateTestAttributes(new byte[] { 1, 1, 1, 1 }); // Different handle in attrs

        // Act
        cache.Set("/test/path", handle, attrs);

        // Assert
        cache.TryGet("/test/path", out var resultHandle, out var resultAttrs);
        resultHandle.Should().BeEquivalentTo(handle); // Should use explicit handle, not the one from attrs
    }

    [Fact]
    public void CleanupExpiredEntries_ShouldRemoveOnlyExpired()
    {
        // Arrange
        using var cache = new FileHandleCache(
            defaultExpiration: TimeSpan.FromMilliseconds(100),
            enableAutoCleanup: false); // Disable auto-cleanup for this test

        cache.Set("/expires-soon", CreateTestAttributes(1), TimeSpan.FromMilliseconds(50));
        cache.Set("/expires-later", CreateTestAttributes(2), TimeSpan.FromSeconds(10));

        // Act - wait for first entry to expire
        Thread.Sleep(100);
        var removed = cache.CleanupExpiredEntries();

        // Assert
        removed.Should().Be(1);
        cache.Contains("/expires-soon").Should().BeFalse();
        cache.Contains("/expires-later").Should().BeTrue();
    }
}
