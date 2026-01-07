namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using NFSLibrary.Rpc;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides stub methods for creating NFSv4 GETATTR operation requests and decoding attribute responses.
    /// The GETATTR operation retrieves file or directory attributes (metadata) such as size, mode, type,
    /// and timestamps. Attributes are requested using a bitmap mask and returned in encoded XDR format.
    /// This class handles both building requests and decoding the attribute responses.
    /// </summary>
    internal class GetattrStub
    {
        /// <summary>
        /// Generates a GETATTR operation request to retrieve file attributes.
        /// The attribute IDs are converted into a bitmap mask that tells the server
        /// which attributes to return in the response.
        /// </summary>
        /// <param name="attrs">List of attribute IDs to retrieve (e.g., FATTR4_SIZE, FATTR4_MODE).</param>
        /// <returns>An NfsArgop4 structure containing the GETATTR operation request.</returns>
        public static NfsArgop4 GenerateRequest(List<int> attrs)
        {
            NfsArgop4 op = new NfsArgop4();
            Getattr4Args args = new Getattr4Args();

            args.Attr_request = new Bitmap4();
            args.Attr_request.Value = new Uint32T[2];
            args.Attr_request.Value[0] = new Uint32T();
            args.Attr_request.Value[1] = new Uint32T();

            foreach (int mask in attrs)
            {
                int bit = mask - (32 * (mask / 32));
                args.Attr_request.Value[mask / 32].Value |= 1 << bit;
            }

            op.Argop = NfsOpnum4.OP_GETATTR;
            op.Opgetattr = args;

            return op;
        }

        /// <summary>
        /// Decodes file attributes from an Fattr4 structure.
        /// Extracts the attribute bitmap to determine which attributes are present,
        /// then decodes each attribute from the XDR-encoded byte stream.
        /// </summary>
        /// <param name="attributes">The Fattr4 structure containing encoded attributes from the server response.</param>
        /// <returns>A dictionary mapping attribute IDs to their decoded values (typed objects like uint64_t, mode4, Nfstime4).</returns>
        public static Dictionary<int, Object> DecodeType(Fattr4 attributes)
        {
            Dictionary<int, Object> attr = new Dictionary<int, Object>();

            int[] mask = new int[attributes.Attrmask.Value.Length];
            for (int i = 0; i < mask.Length; i++)
            {
                mask[i] = attributes.Attrmask.Value[i].Value;
            }

            XdrDecodingStream xdr = new XdrBufferDecodingStream(attributes.Attr_vals.Value);
            xdr.BeginDecoding();

            if (mask.Length != 0)
            {
                int maxAttr = 32 * mask.Length;
                for (int i = 0; i < maxAttr; i++)
                {
                    int newmask = (mask[i / 32] >> (i - (32 * (i / 32))));
                    if ((newmask & 1L) != 0)
                    {
                        xdr2fattr(attr, i, xdr);
                    }
                }
            }

            xdr.EndDecoding();

            return attr;
        }

        /// <summary>
        /// Decodes a specific attribute from XDR stream and adds it to the attribute dictionary.
        /// Currently supports decoding SIZE, MODE, TYPE, and various time attributes.
        /// Unsupported attributes are silently skipped.
        /// </summary>
        /// <param name="attr">The dictionary to add the decoded attribute to.</param>
        /// <param name="fattr">The attribute ID to decode (e.g., FATTR4_SIZE, FATTR4_MODE).</param>
        /// <param name="xdr">The XDR decoding stream positioned at the attribute value.</param>
        private static void xdr2fattr(Dictionary<int, Object> attr, int fattr, XdrDecodingStream xdr)
        {
            switch (fattr)
            {
                case NFSv4Protocol.FATTR4_SIZE:
                    Uint64T size = new Uint64T();
                    size.XdrDecode(xdr);
                    attr.Add(fattr, size);
                    break;

                case NFSv4Protocol.FATTR4_MODE:
                    Mode4 mode = new Mode4();
                    mode.XdrDecode(xdr);
                    attr.Add(fattr, mode);
                    break;

                case NFSv4Protocol.FATTR4_TYPE:
                    Fattr4Type type = new Fattr4Type();
                    type.XdrDecode(xdr);
                    attr.Add(fattr, type);
                    break;

                case NFSv4Protocol.FATTR4_TIME_CREATE:
                    Nfstime4 time = new Nfstime4();
                    time.XdrDecode(xdr);
                    attr.Add(fattr, time);
                    break;

                case NFSv4Protocol.FATTR4_TIME_ACCESS:
                    Nfstime4 time2 = new Nfstime4();
                    time2.XdrDecode(xdr);
                    attr.Add(fattr, time2);
                    break;

                case NFSv4Protocol.FATTR4_TIME_MODIFY:
                    Nfstime4 time3 = new Nfstime4();
                    time3.XdrDecode(xdr);
                    attr.Add(fattr, time3);
                    break;

                // Note: Fattr4Owner and Fattr4OwnerGroup parsing not implemented
                // These would require principal name handling (user@domain format)

                default:
                    break;
            }
        }
    }
}
