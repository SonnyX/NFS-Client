namespace Test.Integration.Runner;

/// <summary>
/// Generates test data for NFS integration tests.
/// </summary>
public static class TestDataGenerator
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generates a unique file name with the given prefix.
    /// </summary>
    public static string GenerateFileName(string prefix = "test", string extension = "txt")
    {
        return $"{prefix}_{Guid.NewGuid():N}.{extension}";
    }

    /// <summary>
    /// Generates a unique directory name.
    /// </summary>
    public static string GenerateDirectoryName(string prefix = "testdir")
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Generates content with a known pattern for verification.
    /// </summary>
    public static byte[] GeneratePatternedContent(int length)
    {
        var buffer = new byte[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = (byte)(i % 256);
        }
        return buffer;
    }

    /// <summary>
    /// Verifies that content matches the expected pattern.
    /// </summary>
    public static bool VerifyPatternedContent(byte[] content, int expectedLength)
    {
        if (content.Length != expectedLength)
        {
            return false;
        }

        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] != (byte)(i % 256))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Generates a small file content (1 KB) for quick tests.
    /// </summary>
    public static byte[] GenerateSmallContent()
    {
        return GeneratePatternedContent(1024);
    }
}
