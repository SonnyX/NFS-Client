/*
 * Automatically generated by jrpcgen 1.0.7 on 27/08/2010
 * jrpcgen is part of the "Remote Tea.Net" ONC/RPC package for C#
 * See http://remotetea.sourceforge.net for details
 */

namespace NFSLibrary.Protocols.V4.RPC.Callback
{
    using org.acplt.oncrpc;

    public class CB_SEQUENCE4resok : XdrAble
    {
        public sessionid4 csr_sessionid;
        public sequenceid4 csr_sequenceid;
        public slotid4 csr_slotid;
        public slotid4 csr_highest_slotid;
        public slotid4 csr_target_highest_slotid;

        public CB_SEQUENCE4resok()
        {
        }

        public CB_SEQUENCE4resok(XdrDecodingStream xdr)
        {
            xdrDecode(xdr);
        }

        public void xdrEncode(XdrEncodingStream xdr)
        {
            csr_sequenceid.xdrEncode(xdr);
            csr_slotid.xdrEncode(xdr);
            csr_highest_slotid.xdrEncode(xdr);
            csr_target_highest_slotid.xdrEncode(xdr);
        }

        public void xdrDecode(XdrDecodingStream xdr)
        {
            csr_sequenceid = new sequenceid4(xdr);
            csr_slotid = new slotid4(xdr);
            csr_highest_slotid = new slotid4(xdr);
            csr_target_highest_slotid = new slotid4(xdr);
        }
    }
} // End of CB_SEQUENCE4resok.cs