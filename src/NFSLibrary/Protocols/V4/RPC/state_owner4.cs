/*
 * Automatically generated by jrpcgen 1.0.7 on 27/08/2010
 * jrpcgen is part of the "Remote Tea.Net" ONC/RPC package for C#
 * See http://remotetea.sourceforge.net for details
 */

namespace NFSLibrary.Protocols.V4.RPC
{
    using org.acplt.oncrpc;

    public class state_owner4 : XdrAble
    {
        public clientid4 clientid;
        public byte[] owner;

        public state_owner4()
        {
        }

        public state_owner4(XdrDecodingStream xdr)
        {
            xdrDecode(xdr);
        }

        public void xdrEncode(XdrEncodingStream xdr)
        {
            clientid.xdrEncode(xdr);
            xdr.xdrEncodeDynamicOpaque(owner);
        }

        public void xdrDecode(XdrDecodingStream xdr)
        {
            clientid = new clientid4(xdr);
            owner = xdr.xdrDecodeDynamicOpaque();
        }
    }
} // End of state_owner4.cs