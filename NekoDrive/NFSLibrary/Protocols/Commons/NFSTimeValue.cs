/*
 * Automatically generated by jrpcgen 1.0.7 on 27/08/2010
 * jrpcgen is part of the "Remote Tea.Net" ONC/RPC package for C#
 * See http://remotetea.sourceforge.net for details
 */
using org.acplt.oncrpc;

namespace NFSLibrary.Protocols.Commons
{
    public class NFSTimeValue : XdrAble
    {
        private int _seconds;
        private int _useconds;

        public NFSTimeValue()
        {
            this._seconds = -1;
            this._useconds = -1;
        }

        public NFSTimeValue(XdrDecodingStream xdr)
        { xdrDecode(xdr); }

        public void xdrEncode(XdrEncodingStream xdr)
        {
            xdr.xdrEncodeInt(this._seconds);
            xdr.xdrEncodeInt(this._useconds);
        }

        public void xdrDecode(XdrDecodingStream xdr)
        {
            this._seconds = xdr.xdrDecodeInt();
            this._useconds = xdr.xdrDecodeInt();
        }

        public int Seconds
        {
            get
            { return this._seconds; }
            set
            { this._seconds = value; }
        }

        public int UnixSeconds
        {
            get
            { return this._useconds; }
            set
            { this._useconds = value; }
        }
    }
    // End of nfstimeval.cs
}