/*
 * Automatically generated by jrpcgen 1.0.7 on 27/08/2010
 * jrpcgen is part of the "Remote Tea.Net" ONC/RPC package for C#
 * See http://remotetea.sourceforge.net for details
 */

namespace NFSLibrary.Protocols.V4.RPC
{
    using org.acplt.oncrpc;

    public class DESTROY_CLIENTID4args : XdrAble
    {
        public clientid4 dca_clientid;

        public DESTROY_CLIENTID4args()
        {
        }

        public DESTROY_CLIENTID4args(XdrDecodingStream xdr)
        {
            xdrDecode(xdr);
        }

        public void xdrEncode(XdrEncodingStream xdr)
        {
            dca_clientid.xdrEncode(xdr);
        }

        public void xdrDecode(XdrDecodingStream xdr)
        {
            dca_clientid = new clientid4(xdr);
        }
    }
} // End of  DESTROY_CLIENTID4args.cs