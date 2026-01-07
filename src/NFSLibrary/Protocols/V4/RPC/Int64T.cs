namespace NFSLibrary.Protocols.V4.RPC
{
    using NFSLibrary.Rpc;

    /// <summary>
    /// Represents a 64-bit signed integer for NFSv4 protocol.
    /// </summary>
    public class Int64T : XdrAble
    {
        /// <summary>
        /// Gets or sets the 64-bit signed integer value.
        /// </summary>
        public long Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Int64T"/> class.
        /// </summary>
        public Int64T()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Int64T"/> class with the specified value.
        /// </summary>
        /// <param name="value">The 64-bit signed integer value.</param>
        public Int64T(long value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Int64T"/> class by decoding from XDR stream.
        /// </summary>
        /// <param name="xdr">The XDR decoding stream.</param>
        public Int64T(XdrDecodingStream xdr)
        {
            XdrDecode(xdr);
        }

        /// <summary>
        /// Encodes the object to XDR stream.
        /// </summary>
        /// <param name="xdr">The XDR encoding stream.</param>
        public void XdrEncode(XdrEncodingStream xdr)
        {
            xdr.XdrEncodeLong(Value);
        }

        /// <summary>
        /// Decodes the object from XDR stream.
        /// </summary>
        /// <param name="xdr">The XDR decoding stream.</param>
        public void XdrDecode(XdrDecodingStream xdr)
        {
            Value = xdr.XdrDecodeLong();
        }
    }
}
