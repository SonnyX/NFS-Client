namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using NFSLibrary.Rpc;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides stub methods for creating NFSv4 SETATTR operation requests.
    /// The SETATTR operation changes file or directory attributes (metadata) such as size,
    /// mode, owner, and timestamps. This implementation currently supports setting the file size,
    /// which is commonly used for truncating or extending files. Requires a valid stateid
    /// from an OPEN operation when modifying certain attributes.
    /// </summary>
    internal class SetAttrStub
    {
        /// <summary>
        /// Generates a SETATTR operation request to set the file size.
        /// This is commonly used to truncate a file to a smaller size or extend it to a larger size.
        /// Requires the file to be opened with appropriate access rights.
        /// </summary>
        /// <param name="stateid">The state ID of the open file obtained from OPEN operation.</param>
        /// <param name="size">The new size for the file in bytes (can be larger or smaller than current size).</param>
        /// <returns>An NfsArgop4 structure containing the SETATTR operation request.</returns>
        public static NfsArgop4 GenerateSetSizeRequest(Stateid4 stateid, long size)
        {
            Setattr4Args args = new Setattr4Args();

            args.Stateid = stateid;

            Fattr4 attr = new Fattr4();
            attr.Attrmask = sizeFattrBitmap();
            attr.Attr_vals = new Attrlist4();
            attr.Attr_vals.Value = encodeSizeAttr(size);

            args.ObjAttributes = attr;

            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_SETATTR;
            op.Opsetattr = args;

            return op;
        }

        /// <summary>
        /// Creates a Bitmap4 structure with the SIZE attribute mask.
        /// The bitmap tells the server which attributes are being set in this operation.
        /// </summary>
        /// <returns>A Bitmap4 structure configured for the SIZE attribute (Fattr4Size).</returns>
        private static Bitmap4 sizeFattrBitmap()
        {
            List<int> attrs = new List<int>();
            attrs.Add(NFSv4Protocol.FATTR4_SIZE);

            Bitmap4 attrBitmap = new Bitmap4();
            attrBitmap.Value = new Uint32T[2];
            attrBitmap.Value[0] = new Uint32T();
            attrBitmap.Value[1] = new Uint32T();

            foreach (int mask in attrs)
            {
                int bit;
                Uint32T bitmap;
                if (mask > 31)
                {
                    bit = mask - 32;
                    bitmap = attrBitmap.Value[1];
                }
                else
                {
                    bit = mask;
                    bitmap = attrBitmap.Value[0];
                }

                bitmap.Value |= 1 << bit;
            }

            return attrBitmap;
        }

        /// <summary>
        /// Encodes a size attribute value into a byte array.
        /// Uses XDR encoding as required by the NFSv4 protocol for attribute values.
        /// </summary>
        /// <param name="size">The size value to encode in bytes.</param>
        /// <returns>A byte array containing the XDR-encoded size attribute.</returns>
        private static byte[] encodeSizeAttr(long size)
        {
            XdrBufferEncodingStream xdr = new XdrBufferEncodingStream(1024);

            xdr.BeginEncoding(null, 0);

            Fattr4Size fsize = new Fattr4Size(new Uint64T(size));
            fsize.XdrEncode(xdr);

            xdr.EndEncoding();

            byte[] retBytes = new byte[xdr.GetXdrLength()];
            Array.Copy(xdr.GetXdrData(), 0, retBytes, 0, xdr.GetXdrLength());

            return retBytes;
        }
    }
}
