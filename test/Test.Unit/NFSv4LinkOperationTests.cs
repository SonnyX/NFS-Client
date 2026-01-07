using FluentAssertions;
using NFSLibrary.Protocols.V4.RPC;
using NFSLibrary.Rpc;

namespace Test.Unit;

/// <summary>
/// Tests for NFSv4 link operation XDR serialization classes.
/// </summary>
public class NFSv4LinkOperationTests
{
    #region Link4Args XDR Tests

    [Fact]
    public void LINK4args_XdrEncode_EncodesNewname()
    {
        // Arrange
        var args = new Link4Args();
        args.Newname = new Component4();
        args.Newname.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes("testlink")));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);

        // Act
        args.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Assert
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decoded = new Link4Args(decodingStream);
        decodingStream.EndDecoding();

        decoded.Newname.Should().NotBeNull();
        System.Text.Encoding.UTF8.GetString(decoded.Newname.Value.Value.Value).Should().Be("testlink");
    }

    [Fact]
    public void LINK4args_XdrDecode_DecodesNewname()
    {
        // Arrange
        var originalArgs = new Link4Args();
        originalArgs.Newname = new Component4();
        originalArgs.Newname.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes("mylink")));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);
        originalArgs.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Act
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decodedArgs = new Link4Args(decodingStream);
        decodingStream.EndDecoding();

        // Assert
        decodedArgs.Newname.Should().NotBeNull();
        var linkName = System.Text.Encoding.UTF8.GetString(decodedArgs.Newname.Value.Value.Value);
        linkName.Should().Be("mylink");
    }

    [Fact]
    public void LINK4args_RoundTrip_WithSpecialCharacters()
    {
        // Arrange
        var originalArgs = new Link4Args();
        originalArgs.Newname = new Component4();
        originalArgs.Newname.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes("link-with_special.chars")));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);
        originalArgs.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Act
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decodedArgs = new Link4Args(decodingStream);
        decodingStream.EndDecoding();

        // Assert
        var linkName = System.Text.Encoding.UTF8.GetString(decodedArgs.Newname.Value.Value.Value);
        linkName.Should().Be("link-with_special.chars");
    }

    #endregion

    #region Readlink4Resok XDR Tests

    [Fact]
    public void READLINK4resok_XdrEncode_EncodesLink()
    {
        // Arrange
        var resok = new Readlink4Resok();
        resok.Link = new Linktext4();
        resok.Link.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes("/path/to/target")));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);

        // Act
        resok.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Assert
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decoded = new Readlink4Resok(decodingStream);
        decodingStream.EndDecoding();

        decoded.Link.Should().NotBeNull();
        System.Text.Encoding.UTF8.GetString(decoded.Link.Value.Value.Value).Should().Be("/path/to/target");
    }

    [Fact]
    public void READLINK4resok_XdrDecode_DecodesLink()
    {
        // Arrange
        var originalResok = new Readlink4Resok();
        originalResok.Link = new Linktext4();
        originalResok.Link.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes("../relative/path")));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);
        originalResok.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Act
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decoded = new Readlink4Resok(decodingStream);
        decodingStream.EndDecoding();

        // Assert
        decoded.Link.Should().NotBeNull();
        var targetPath = System.Text.Encoding.UTF8.GetString(decoded.Link.Value.Value.Value);
        targetPath.Should().Be("../relative/path");
    }

    [Fact]
    public void READLINK4resok_RoundTrip_WithLongPath()
    {
        // Arrange
        var longPath = "/very/long/path/to/some/deeply/nested/directory/structure/file.txt";
        var resok = new Readlink4Resok();
        resok.Link = new Linktext4();
        resok.Link.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes(longPath)));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);
        resok.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Act
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decoded = new Readlink4Resok(decodingStream);
        decodingStream.EndDecoding();

        // Assert
        var decodedPath = System.Text.Encoding.UTF8.GetString(decoded.Link.Value.Value.Value);
        decodedPath.Should().Be(longPath);
    }

    #endregion

    #region Linktext4 Tests

    [Fact]
    public void linktext4_RoundTrip_PreservesValue()
    {
        // Arrange
        var original = new Linktext4();
        original.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes("/some/path/to/file")));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);
        original.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Act
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decoded = new Linktext4(decodingStream);
        decodingStream.EndDecoding();

        // Assert
        var decodedPath = System.Text.Encoding.UTF8.GetString(decoded.Value.Value.Value);
        decodedPath.Should().Be("/some/path/to/file");
    }

    [Fact]
    public void linktext4_WithUnicodePath_PreservesValue()
    {
        // Arrange - test with unicode characters in path
        var pathWithUnicode = "/home/usuario/fichero.txt";
        var original = new Linktext4();
        original.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes(pathWithUnicode)));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);
        original.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Act
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decoded = new Linktext4(decodingStream);
        decodingStream.EndDecoding();

        // Assert
        var decodedPath = System.Text.Encoding.UTF8.GetString(decoded.Value.Value.Value);
        decodedPath.Should().Be(pathWithUnicode);
    }

    #endregion

    #region Component4 Tests

    [Fact]
    public void component4_RoundTrip_PreservesValue()
    {
        // Arrange
        var original = new Component4();
        original.Value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes("filename.txt")));

        var buffer = new byte[1024];
        var encodingStream = new XdrBufferEncodingStream(buffer);
        original.XdrEncode(encodingStream);
        encodingStream.EndEncoding();

        // Act
        var decodingStream = new XdrBufferDecodingStream(buffer);
        decodingStream.BeginDecoding();
        var decoded = new Component4(decodingStream);
        decodingStream.EndDecoding();

        // Assert
        var decodedName = System.Text.Encoding.UTF8.GetString(decoded.Value.Value.Value);
        decodedName.Should().Be("filename.txt");
    }

    [Fact]
    public void component4_WithConstructor_CreatesValidInstance()
    {
        // Arrange & Act
        var value = new Utf8strCs(new Utf8string(System.Text.Encoding.UTF8.GetBytes("testfile")));
        var component = new Component4(value);

        // Assert
        component.Value.Should().NotBeNull();
        var name = System.Text.Encoding.UTF8.GetString(component.Value.Value.Value);
        name.Should().Be("testfile");
    }

    #endregion
}
