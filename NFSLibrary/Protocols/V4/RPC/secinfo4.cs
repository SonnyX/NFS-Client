/*
 * Automatically generated by jrpcgen 1.0.7 on 27/08/2010
 * jrpcgen is part of the "Remote Tea.Net" ONC/RPC package for C#
 * See http://remotetea.sourceforge.net for details
 */

namespace NFSLibrary.Protocols.V4.RPC
{
    using org.acplt.oncrpc;

    public class secinfo4 : XdrAble
    {
        public int flavor;
        public rpcsec_gss_info flavor_info;

        public secinfo4()
        {
        }

        public secinfo4(XdrDecodingStream xdr)
        {
            xdrDecode(xdr);
        }

        public void xdrEncode(XdrEncodingStream xdr)
        {
            xdr.xdrEncodeInt(flavor);
            switch (flavor)
            {
                case auth_flavor.RPCSEC_GSS:
                    flavor_info.xdrEncode(xdr);
                    break;

                default:
                    break;
            }
        }

        public void xdrDecode(XdrDecodingStream xdr)
        {
            flavor = xdr.xdrDecodeInt();
            switch (flavor)
            {
                case auth_flavor.RPCSEC_GSS:
                    flavor_info = new rpcsec_gss_info(xdr);
                    break;

                default:
                    break;
            }
        }
    }
} // End of secinfo4.cs