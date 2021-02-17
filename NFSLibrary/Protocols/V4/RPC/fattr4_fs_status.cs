/*
 * Automatically generated by jrpcgen 1.0.7 on 27/08/2010
 * jrpcgen is part of the "Remote Tea.Net" ONC/RPC package for C#
 * See http://remotetea.sourceforge.net for details
 */

namespace NFSLibrary.Protocols.V4.RPC
{
    using org.acplt.oncrpc;

    public class fattr4_fs_status : XdrAble
    {
        public fs4_status value;

        public fattr4_fs_status()
        {
        }

        public fattr4_fs_status(fs4_status value)
        {
            this.value = value;
        }

        public fattr4_fs_status(XdrDecodingStream xdr)
        {
            xdrDecode(xdr);
        }

        public void xdrEncode(XdrEncodingStream xdr)
        {
            value.xdrEncode(xdr);
        }

        public void xdrDecode(XdrDecodingStream xdr)
        {
            value = new fs4_status(xdr);
        }
    }
}

// End of fattr4_fs_status.cs