namespace NFSLibrary.Protocols.V4.RPC
{
    using NFSLibrary.Rpc;

    /// <summary>
    /// Represents a 32-bit unsigned integer in NFSv4 protocol.
    /// </summary>
    public class Uint32T : XdrAble
    {
        /// <summary>
        /// Gets or sets the 32-bit unsigned integer Value.
        /// </summary>
        public int Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Uint32T"/> class.
        /// </summary>
        public Uint32T()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Uint32T"/> class with the specified Value.
        /// </summary>
        /// <param name="Value">The 32-bit unsigned integer Value.</param>
        public Uint32T(int Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Uint32T"/> class by decoding from XDR stream.
        /// </summary>
        /// <param name="xdr">The XDR decoding stream.</param>
        public Uint32T(XdrDecodingStream xdr)
        {
            XdrDecode(xdr);
        }

        /// <summary>
        /// Encodes the object to XDR stream.
        /// </summary>
        /// <param name="xdr">The XDR encoding stream.</param>
        public void XdrEncode(XdrEncodingStream xdr)
        {
            xdr.XdrEncodeInt(Value);
        }

        /// <summary>
        /// Decodes the object from XDR stream.
        /// </summary>
        /// <param name="xdr">The XDR decoding stream.</param>
        public void XdrDecode(XdrDecodingStream xdr)
        {
            Value = xdr.XdrDecodeInt();
        }
    }
}
