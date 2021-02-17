/*
 * Automatically generated by jrpcgen 1.0.7 on 27/08/2010
 * jrpcgen is part of the "Remote Tea.Net" ONC/RPC package for C#
 * See http://remotetea.sourceforge.net for details
 */

namespace NFSLibrary.Protocols.V4.RPC
{
    using org.acplt.oncrpc;

    public class retention_set4 : XdrAble
    {
        public bool rs_enable;
        public int[] rs_duration;

        public retention_set4()
        {
        }

        public retention_set4(XdrDecodingStream xdr)
        {
            xdrDecode(xdr);
        }

        public void xdrEncode(XdrEncodingStream xdr)
        {
            xdr.xdrEncodeIntVector(rs_duration);
        }

        public void xdrDecode(XdrDecodingStream xdr)
        {
            rs_duration = xdr.xdrDecodeIntVector();
        }
    }
} // End of retention_set4.cs