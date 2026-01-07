namespace Test.Integration.Helpers;

/// <summary>
/// Generates test data for NFS integration tests.
/// </summary>
public static class TestDataGenerator
{
    private static readonly Random Random = new();

    /// <summary>
    /// Known content of the pre-seeded testfile.txt.
    /// </summary>
    public const string TestFileContent = """
        This is a test file for NFS integration testing.
        It contains multiple lines of text content.

        Line 4 is here.
        Line 5 is the last line.

        """;

    /// <summary>
    /// Known content of the pre-seeded nested.txt.
    /// </summary>
    public const string NestedFileContent = """
        This is a nested file in a subdirectory.
        Used for testing path traversal and directory operations.

        """;

    /// <summary>
    /// Generates a unique file name with the given prefix.
    /// </summary>
    /// <param name="prefix">Prefix for the file name.</param>
    /// <param name="extension">File extension (without dot).</param>
    public static string GenerateFileName(string prefix = "test", string extension = "txt")
    {
        return $"{prefix}_{Guid.NewGuid():N}.{extension}";
    }

    /// <summary>
    /// Generates a unique directory name.
    /// </summary>
    /// <param name="prefix">Prefix for the directory name.</param>
    public static string GenerateDirectoryName(string prefix = "testdir")
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Generates random text content of specified length.
    /// </summary>
    /// <param name="length">Number of characters to generate.</param>
    public static string GenerateTextContent(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 \n";
        var buffer = new char[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = chars[Random.Next(chars.Length)];
        }
        return new string(buffer);
    }

    /// <summary>
    /// Generates random binary content of specified length.
    /// </summary>
    /// <param name="length">Number of bytes to generate.</param>
    public static byte[] GenerateBinaryContent(int length)
    {
        var buffer = new byte[length];
        Random.NextBytes(buffer);
        return buffer;
    }

    /// <summary>
    /// Generates content with a known pattern for verification.
    /// </summary>
    /// <param name="length">Number of bytes to generate.</param>
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
    /// <param name="content">Content to verify.</param>
    /// <param name="expectedLength">Expected length of the content.</param>
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
    /// Creates a temporary local file with random content.
    /// Returns the path to the file.
    /// </summary>
    /// <param name="size">Size of the file in bytes.</param>
    public static string CreateTempFile(int size)
    {
        var path = Path.GetTempFileName();
        var content = GenerateBinaryContent(size);
        File.WriteAllBytes(path, content);
        return path;
    }

    /// <summary>
    /// Creates a temporary local file with specific content.
    /// Returns the path to the file.
    /// </summary>
    /// <param name="content">Content to write to the file.</param>
    public static string CreateTempFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Creates a temporary local file with specific binary content.
    /// Returns the path to the file.
    /// </summary>
    /// <param name="content">Content to write to the file.</param>
    public static string CreateTempFile(byte[] content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, content);
        return path;
    }

    /// <summary>
    /// Generates a large file content (1 MB) for performance testing.
    /// </summary>
    public static byte[] GenerateLargeContent()
    {
        return GeneratePatternedContent(1024 * 1024); // 1 MB
    }

    /// <summary>
    /// Generates a small file content (1 KB) for quick tests.
    /// </summary>
    public static byte[] GenerateSmallContent()
    {
        return GeneratePatternedContent(1024); // 1 KB
    }
}
