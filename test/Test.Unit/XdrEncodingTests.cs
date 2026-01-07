using FluentAssertions;
using NFSLibrary.Rpc;

namespace Test.Unit;

/// <summary>
/// Unit tests for XDR (External Data Representation) encoding and decoding streams.
/// Tests the XdrBufferEncodingStream and XdrBufferDecodingStream classes.
/// </summary>
public class XdrEncodingTests
{
    #region XdrBufferEncodingStream Constructor Tests

    [Fact]
    public void XdrBufferEncodingStream_ValidBufferSize_CreatesStream()
    {
        // Arrange & Act
        var stream = new XdrBufferEncodingStream(64);

        // Assert
        stream.Should().NotBeNull();
        stream.GetXdrLength().Should().Be(0);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(1024)]
    public void XdrBufferEncodingStream_MultipleOfFour_CreatesStream(int size)
    {
        // Arrange & Act
        var stream = new XdrBufferEncodingStream(size);

        // Assert
        stream.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public void XdrBufferEncodingStream_NotMultipleOfFour_ThrowsArgumentException(int size)
    {
        // Arrange & Act
        var act = () => new XdrBufferEncodingStream(size);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*multiple of four*");
    }

    [Fact]
    public void XdrBufferEncodingStream_NegativeSize_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new XdrBufferEncodingStream(-4);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void XdrBufferEncodingStream_WithExistingBuffer_UsesBuffer()
    {
        // Arrange
        var buffer = new byte[64];

        // Act
        var stream = new XdrBufferEncodingStream(buffer);

        // Assert
        stream.GetXdrData().Should().BeSameAs(buffer);
    }

    [Fact]
    public void XdrBufferEncodingStream_WithExistingBuffer_NotMultipleOfFour_ThrowsArgumentException()
    {
        // Arrange
        var buffer = new byte[63];

        // Act
        var act = () => new XdrBufferEncodingStream(buffer);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*multiple of four*");
    }

    #endregion

    #region XdrBufferDecodingStream Constructor Tests

    [Fact]
    public void XdrBufferDecodingStream_ValidBuffer_CreatesStream()
    {
        // Arrange
        var buffer = new byte[64];

        // Act
        var stream = new XdrBufferDecodingStream(buffer, 64);

        // Assert
        stream.Should().NotBeNull();
    }

    [Fact]
    public void XdrBufferDecodingStream_BufferOnly_UsesFullLength()
    {
        // Arrange
        var buffer = new byte[64];

        // Act
        var stream = new XdrBufferDecodingStream(buffer);

        // Assert
        stream.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void XdrBufferDecodingStream_LengthNotMultipleOfFour_ThrowsArgumentException(int length)
    {
        // Arrange
        var buffer = new byte[64];

        // Act
        var act = () => new XdrBufferDecodingStream(buffer, length);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*multiple of four*");
    }

    [Fact]
    public void XdrBufferDecodingStream_NegativeLength_ThrowsArgumentException()
    {
        // Arrange
        var buffer = new byte[64];

        // Act
        var act = () => new XdrBufferDecodingStream(buffer, -4);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*negative*");
    }

    #endregion

    #region Integer Encoding/Decoding Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void XdrInt_EncodeDecode_RoundTrips(int value)
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(8);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeInt(value);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeInt();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void XdrInt_EncodesAsBigEndian()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(8);
        stream.BeginEncoding(null, 0);

        // Act - Encode 0x12345678
        stream.XdrEncodeInt(0x12345678);

        // Assert - Should be big endian
        var data = stream.GetXdrData();
        data[0].Should().Be(0x12);
        data[1].Should().Be(0x34);
        data[2].Should().Be(0x56);
        data[3].Should().Be(0x78);
    }

    [Fact]
    public void XdrInt_MultipleInts_EncodesSequentially()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(16);
        stream.BeginEncoding(null, 0);

        // Act
        stream.XdrEncodeInt(1);
        stream.XdrEncodeInt(2);
        stream.XdrEncodeInt(3);

        // Assert
        stream.GetXdrLength().Should().Be(12); // 3 ints * 4 bytes

        var decodeStream = new XdrBufferDecodingStream(stream.GetXdrData(), stream.GetXdrLength());
        decodeStream.BeginDecoding();
        decodeStream.XdrDecodeInt().Should().Be(1);
        decodeStream.XdrDecodeInt().Should().Be(2);
        decodeStream.XdrDecodeInt().Should().Be(3);
    }

    #endregion

    #region Long (Hyper) Encoding/Decoding Tests

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(0x123456789ABCDEF0L)]
    public void XdrLong_EncodeDecode_RoundTrips(long value)
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(16);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeLong(value);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeLong();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void XdrLong_Takes8Bytes()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(16);
        stream.BeginEncoding(null, 0);

        // Act
        stream.XdrEncodeLong(123456789L);

        // Assert
        stream.GetXdrLength().Should().Be(8);
    }

    #endregion

    #region Boolean Encoding/Decoding Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void XdrBoolean_EncodeDecode_RoundTrips(bool value)
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(8);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeBoolean(value);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeBoolean();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void XdrBoolean_True_EncodesAsOne()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(8);
        stream.BeginEncoding(null, 0);

        // Act
        stream.XdrEncodeBoolean(true);

        // Assert
        var decodeStream = new XdrBufferDecodingStream(stream.GetXdrData(), stream.GetXdrLength());
        decodeStream.BeginDecoding();
        decodeStream.XdrDecodeInt().Should().Be(1);
    }

    [Fact]
    public void XdrBoolean_False_EncodesAsZero()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(8);
        stream.BeginEncoding(null, 0);

        // Act
        stream.XdrEncodeBoolean(false);

        // Assert
        var decodeStream = new XdrBufferDecodingStream(stream.GetXdrData(), stream.GetXdrLength());
        decodeStream.BeginDecoding();
        decodeStream.XdrDecodeInt().Should().Be(0);
    }

    #endregion

    #region Float Encoding/Decoding Tests

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(3.14159f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    public void XdrFloat_EncodeDecode_RoundTrips(float value)
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(8);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeFloat(value);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeFloat();

        // Assert
        result.Should().Be(value);
    }

    #endregion

    #region Double Encoding/Decoding Tests

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(3.14159265358979)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void XdrDouble_EncodeDecode_RoundTrips(double value)
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(16);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeDouble(value);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeDouble();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void XdrDouble_Takes8Bytes()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(16);
        stream.BeginEncoding(null, 0);

        // Act
        stream.XdrEncodeDouble(3.14159);

        // Assert
        stream.GetXdrLength().Should().Be(8);
    }

    #endregion

    #region Short Encoding/Decoding Tests

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)1)]
    [InlineData((short)-1)]
    [InlineData(short.MaxValue)]
    [InlineData(short.MinValue)]
    public void XdrShort_EncodeDecode_RoundTrips(short value)
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(8);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeShort(value);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeShort();

        // Assert
        result.Should().Be(value);
    }

    #endregion

    #region Byte Encoding/Decoding Tests

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    [InlineData((byte)127)]
    [InlineData((byte)255)]
    public void XdrByte_EncodeDecode_RoundTrips(byte value)
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(8);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeByte(value);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeByte();

        // Assert
        result.Should().Be(value);
    }

    #endregion

    #region Opaque Data Encoding/Decoding Tests

    [Fact]
    public void XdrOpaque_EncodeDecode_RoundTrips()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeOpaque(data);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeOpaque(data.Length);

        // Assert
        result.Should().BeEquivalentTo(data);
    }

    [Theory]
    [InlineData(1, 4)]   // 1 byte + 3 padding
    [InlineData(2, 4)]   // 2 bytes + 2 padding
    [InlineData(3, 4)]   // 3 bytes + 1 padding
    [InlineData(4, 4)]   // 4 bytes, no padding
    [InlineData(5, 8)]   // 5 bytes + 3 padding
    [InlineData(8, 8)]   // 8 bytes, no padding
    public void XdrOpaque_PadsToMultipleOfFour(int dataLength, int expectedEncodedLength)
    {
        // Arrange
        var data = new byte[dataLength];
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act
        encodeStream.XdrEncodeOpaque(data);

        // Assert
        encodeStream.GetXdrLength().Should().Be(expectedEncodedLength);
    }

    [Fact]
    public void XdrOpaque_EmptyArray_EncodesAsZeroBytes()
    {
        // Arrange
        var data = Array.Empty<byte>();
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act
        encodeStream.XdrEncodeOpaque(data);

        // Assert
        encodeStream.GetXdrLength().Should().Be(0);
    }

    [Fact]
    public void XdrDynamicOpaque_EncodeDecode_RoundTrips()
    {
        // Arrange
        var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE };
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeDynamicOpaque(data);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeDynamicOpaque();

        // Assert
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void XdrDynamicOpaque_IncludesLengthPrefix()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03 }; // 3 bytes
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode with length
        encodeStream.XdrEncodeDynamicOpaque(data);

        // Assert - 4 bytes for length + 4 bytes for data (with 1 byte padding)
        encodeStream.GetXdrLength().Should().Be(8);
    }

    #endregion

    #region String Encoding/Decoding Tests

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("hello")]
    [InlineData("Hello, World!")]
    [InlineData("test string with spaces")]
    public void XdrString_EncodeDecode_RoundTrips(string value)
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(256);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeString(value);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeString();

        // Assert
        result.Should().Be(value);
    }

    #endregion

    #region Vector Encoding/Decoding Tests

    [Fact]
    public void XdrIntVector_EncodeDecode_RoundTrips()
    {
        // Arrange
        var values = new int[] { 1, 2, 3, 4, 5 };
        var encodeStream = new XdrBufferEncodingStream(256);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeIntVector(values);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeIntVector();

        // Assert
        result.Should().BeEquivalentTo(values);
    }

    [Fact]
    public void XdrLongVector_EncodeDecode_RoundTrips()
    {
        // Arrange
        var values = new long[] { 100L, 200L, 300L };
        var encodeStream = new XdrBufferEncodingStream(256);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeLongVector(values);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeLongVector();

        // Assert
        result.Should().BeEquivalentTo(values);
    }

    [Fact]
    public void XdrBooleanVector_EncodeDecode_RoundTrips()
    {
        // Arrange
        var values = new bool[] { true, false, true, true, false };
        var encodeStream = new XdrBufferEncodingStream(256);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeBooleanVector(values);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeBooleanVector();

        // Assert
        result.Should().BeEquivalentTo(values);
    }

    [Fact]
    public void XdrStringVector_EncodeDecode_RoundTrips()
    {
        // Arrange
        var values = new string[] { "one", "two", "three" };
        var encodeStream = new XdrBufferEncodingStream(256);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode
        encodeStream.XdrEncodeStringVector(values);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Act - Decode
        var result = decodeStream.XdrDecodeStringVector();

        // Assert
        result.Should().BeEquivalentTo(values);
    }

    #endregion

    #region Fixed Vector Tests

    [Fact]
    public void XdrIntFixedVector_CorrectLength_Succeeds()
    {
        // Arrange
        var values = new int[] { 1, 2, 3 };
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act & Assert - should not throw
        encodeStream.XdrEncodeIntFixedVector(values, 3);
    }

    [Fact]
    public void XdrIntFixedVector_WrongLength_ThrowsArgumentException()
    {
        // Arrange
        var values = new int[] { 1, 2, 3 };
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act
        var act = () => encodeStream.XdrEncodeIntFixedVector(values, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not match*");
    }

    [Fact]
    public void XdrOpaqueFixed_CorrectLength_Succeeds()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4 };
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act & Assert - should not throw
        encodeStream.XdrEncodeOpaque(data, 4);
    }

    [Fact]
    public void XdrOpaqueFixed_WrongLength_ThrowsArgumentException()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4 };
        var encodeStream = new XdrBufferEncodingStream(64);
        encodeStream.BeginEncoding(null, 0);

        // Act
        var act = () => encodeStream.XdrEncodeOpaque(data, 8);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not match*");
    }

    #endregion

    #region Buffer Overflow/Underflow Tests

    [Fact]
    public void XdrBufferEncodingStream_BufferOverflow_ThrowsOncRpcException()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(4); // Only room for 1 int
        stream.BeginEncoding(null, 0);
        stream.XdrEncodeInt(1); // Fill the buffer

        // Act
        var act = () => stream.XdrEncodeInt(2); // Should overflow

        // Assert
        act.Should().Throw<OncRpcException>();
    }

    [Fact]
    public void XdrBufferDecodingStream_BufferUnderflow_ThrowsOncRpcException()
    {
        // Arrange
        var buffer = new byte[] { 0, 0, 0, 1 }; // Only 1 int
        var stream = new XdrBufferDecodingStream(buffer, 4);
        stream.BeginDecoding();
        stream.XdrDecodeInt(); // Read the only int

        // Act
        var act = () => stream.XdrDecodeInt(); // Should underflow

        // Assert
        act.Should().Throw<OncRpcException>();
    }

    #endregion

    #region BeginEncoding/EndEncoding Tests

    [Fact]
    public void XdrBufferEncodingStream_BeginEncoding_ResetsBufferIndex()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(16);
        stream.BeginEncoding(null, 0);
        stream.XdrEncodeInt(1);
        stream.XdrEncodeInt(2);

        // Act
        stream.BeginEncoding(null, 0);

        // Assert
        stream.GetXdrLength().Should().Be(0);
    }

    [Fact]
    public void XdrBufferDecodingStream_BeginDecoding_ResetsBufferIndex()
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(16);
        encodeStream.BeginEncoding(null, 0);
        encodeStream.XdrEncodeInt(42);
        encodeStream.XdrEncodeInt(99);

        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();
        decodeStream.XdrDecodeInt(); // Read first int

        // Act - Reset
        decodeStream.BeginDecoding();
        var result = decodeStream.XdrDecodeInt();

        // Assert - Should read first int again
        result.Should().Be(42);
    }

    #endregion

    #region Close Tests

    [Fact]
    public void XdrBufferEncodingStream_Close_ClearsBuffer()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(16);
        stream.BeginEncoding(null, 0);
        stream.XdrEncodeInt(1);

        // Act
        stream.Close();

        // Assert
        stream.GetXdrData().Should().BeNull();
    }

    [Fact]
    public void XdrBufferDecodingStream_Close_ClearsBuffer()
    {
        // Arrange
        var buffer = new byte[16];
        var stream = new XdrBufferDecodingStream(buffer, 16);

        // Act
        stream.Close();

        // Assert - Further operations should fail
        var act = () =>
        {
            stream.BeginDecoding();
            stream.XdrDecodeInt();
        };
        act.Should().Throw<Exception>();
    }

    #endregion

    #region Character Encoding Tests

    [Fact]
    public void XdrEncodingStream_SetCharacterEncoding_StoresEncoding()
    {
        // Arrange
        var stream = new XdrBufferEncodingStream(64);

        // Act
        stream.SetCharacterEncoding("UTF-8");

        // Assert
        stream.GetCharacterEncoding().Should().Be("UTF-8");
    }

    [Fact]
    public void XdrEncodingStream_DefaultEncoding_IsNull()
    {
        // Arrange & Act
        var stream = new XdrBufferEncodingStream(64);

        // Assert
        stream.GetCharacterEncoding().Should().BeNull();
    }

    #endregion

    #region Complex Mixed Types Tests

    [Fact]
    public void XdrStream_MixedTypes_EncodeDecode_RoundTrips()
    {
        // Arrange
        var encodeStream = new XdrBufferEncodingStream(256);
        encodeStream.BeginEncoding(null, 0);

        // Act - Encode various types
        encodeStream.XdrEncodeInt(42);
        encodeStream.XdrEncodeLong(123456789L);
        encodeStream.XdrEncodeBoolean(true);
        encodeStream.XdrEncodeString("test");
        encodeStream.XdrEncodeDouble(3.14159);
        encodeStream.EndEncoding();

        // Arrange - Decode
        var decodeStream = new XdrBufferDecodingStream(encodeStream.GetXdrData(), encodeStream.GetXdrLength());
        decodeStream.BeginDecoding();

        // Assert - All values round-trip correctly
        decodeStream.XdrDecodeInt().Should().Be(42);
        decodeStream.XdrDecodeLong().Should().Be(123456789L);
        decodeStream.XdrDecodeBoolean().Should().BeTrue();
        decodeStream.XdrDecodeString().Should().Be("test");
        decodeStream.XdrDecodeDouble().Should().Be(3.14159);
    }

    #endregion
}
