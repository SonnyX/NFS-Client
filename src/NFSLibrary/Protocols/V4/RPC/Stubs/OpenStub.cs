namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using NFSLibrary.Rpc;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides stub methods for creating NFSv4 OPEN operation requests.
    /// The OPEN operation establishes access to a regular file, creating it if requested.
    /// It returns a stateid that must be used in subsequent READ, WRITE, and CLOSE operations.
    /// This class provides methods for different open scenarios: reading existing files,
    /// creating new files, and opening files without creation.
    /// </summary>
    internal class OpenStub
    {
        /// <summary>
        /// Generates an OPEN operation request for read access to an existing file.
        /// This operation will fail if the file does not exist (OPEN4_NOCREATE).
        /// </summary>
        /// <param name="path">The name of the file to open in the current directory.</param>
        /// <param name="sequenceId">The sequence ID for the operation.</param>
        /// <param name="clientid">The client ID obtained from EXCHANGE_ID.</param>
        /// <param name="access">The access mode flags (parameter is present but overridden to READ).</param>
        /// <returns>An NfsArgop4 structure containing the OPEN operation request.</returns>
        public static NfsArgop4 NormalREAD(String path, int sequenceId,
        Clientid4 clientid, int access)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_OPEN;
            op.Opopen = new Open4Args();

            op.Opopen.Seqid = new Seqid4(new Uint32T(sequenceId));

            // if ((access & nfs4_prot.OPEN4_SHARE_ACCESS_WANT_DELEG_MASK) == 0){
            // access |= nfs4_prot.OPEN4_SHARE_ACCESS_WANT_NO_DELEG;
            // }
            // op.Opopen.Share_access = new Uint32T(access);
            op.Opopen.Share_access = new Uint32T(NFSv4Protocol.OPEN4_SHARE_ACCESS_READ);
            op.Opopen.Share_deny = new Uint32T(NFSv4Protocol.OPEN4_SHARE_DENY_NONE);

            StateOwner4 owner = new StateOwner4();
            owner.Clientid = clientid;

            owner.Owner = encoding.GetBytes("nfsclient");
            op.Opopen.Owner = new OpenOwner4(owner);

            Openflag4 flag = new Openflag4();
            flag.Opentype = Opentype4.OPEN4_NOCREATE;

            Createhow4 how = new Createhow4();
            how.Mode = Createmode4.UNCHECKED4;
            flag.How = how;
            op.Opopen.Openhow = flag;

            OpenClaim4 claim = new OpenClaim4();
            claim.Claim = OpenClaimType4.CLAIM_NULL;
            claim.File = new Component4(new Utf8strCs(new Utf8string(encoding.GetBytes(path))));
            claim.Delegate_type = NFSv4Protocol.OPEN4_SHARE_ACCESS_WANT_NO_DELEG;
            claim.File_delegate_prev = null;
            claim.Oc_delegate_stateid = null;
            claim.Delegate_type = 0;
            claim.Delegate_cur_info = null;

            op.Opopen.Claim = claim;

            return op;
        }

        /// <summary>
        /// Generates an OPEN operation request to create a new file.
        /// Uses GUARDED4 mode to ensure the file doesn't already exist.
        /// Creates the file with default attributes (mode 0777).
        /// </summary>
        /// <param name="path">The name of the file to create in the current directory.</param>
        /// <param name="sequenceId">The sequence ID for the operation.</param>
        /// <param name="clientid">The client ID obtained from EXCHANGE_ID.</param>
        /// <param name="access">The access mode flags (e.g., OPEN4_SHARE_ACCESS_WRITE).</param>
        /// <returns>An NfsArgop4 structure containing the OPEN operation request.</returns>
        public static NfsArgop4 NormalCREATE(String path, int sequenceId,
        Clientid4 clientid, int access)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_OPEN;
            op.Opopen = new Open4Args();

            op.Opopen.Seqid = new Seqid4(new Uint32T(sequenceId));

            StateOwner4 owner = new StateOwner4();
            owner.Clientid = clientid;
            owner.Owner = encoding.GetBytes("nfsclient");
            op.Opopen.Owner = new OpenOwner4(owner);

            // Explicitly request no delegation to avoid issues with servers that require delegation handling
            if ((access & NFSv4Protocol.OPEN4_SHARE_ACCESS_WANT_DELEG_MASK) == 0)
            {
                access |= NFSv4Protocol.OPEN4_SHARE_ACCESS_WANT_NO_DELEG;
            }
            op.Opopen.Share_access = new Uint32T(access);
            op.Opopen.Share_deny = new Uint32T(NFSv4Protocol.OPEN4_SHARE_DENY_NONE);

            Openflag4 flag = new Openflag4();
            flag.Opentype = Opentype4.OPEN4_CREATE;

            // Createhow4(mode, attrs, verifier)
            Createhow4 how = new Createhow4();
            how.Mode = Createmode4.GUARDED4;
            //how.mode = Createmode4.EXCLUSIVE4_1;
            Fattr4 attr = new Fattr4();

            attr.Attrmask = OpenFattrBitmap();
            attr.Attr_vals = new Attrlist4();
            attr.Attr_vals.Value = OpenAttrs(7, 7, 7, 0);

            how.Createattrs = attr;
            how.Createverf = new Verifier4(0);  //it is long
            //how.mode = Createmode4.GUARDED4;

            flag.How = how;
            op.Opopen.Openhow = flag;

            OpenClaim4 claim = new OpenClaim4();
            claim.Claim = OpenClaimType4.CLAIM_NULL;
            claim.File = new Component4(new Utf8strCs(new Utf8string(encoding
                    .GetBytes(path))));
            claim.Delegate_type = NFSv4Protocol.OPEN4_SHARE_ACCESS_WANT_NO_DELEG;
            claim.File_delegate_prev = null;
            claim.Oc_delegate_stateid = null;
            claim.Delegate_type = 0;
            claim.Delegate_cur_info = null;

            op.Opopen.Claim = claim;

            return op;
        }

        /// <summary>
        /// Generates an OPEN operation request for opening an existing file without creating it.
        /// Similar to normalREAD but allows specifying custom access flags.
        /// Uses OPEN4_NOCREATE to ensure the file must already exist.
        /// </summary>
        /// <param name="path">The name of the file to open in the current directory.</param>
        /// <param name="sequenceId">The sequence ID for the operation.</param>
        /// <param name="clientid">The client ID obtained from EXCHANGE_ID.</param>
        /// <param name="access">The access mode flags (e.g., OPEN4_SHARE_ACCESS_READ or WRITE).</param>
        /// <returns>An NfsArgop4 structure containing the OPEN operation request.</returns>
        public static NfsArgop4 NormalOPENonly(String path, int sequenceId,
 Clientid4 clientid, int access)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_OPEN;
            op.Opopen = new Open4Args();

            op.Opopen.Seqid = new Seqid4(new Uint32T(sequenceId));

            StateOwner4 owner = new StateOwner4();
            owner.Clientid = clientid;
            owner.Owner = encoding.GetBytes("nfsclient");
            op.Opopen.Owner = new OpenOwner4(owner);

            // Explicitly request no delegation to avoid issues with servers that require delegation handling
            if ((access & NFSv4Protocol.OPEN4_SHARE_ACCESS_WANT_DELEG_MASK) == 0)
            {
                access |= NFSv4Protocol.OPEN4_SHARE_ACCESS_WANT_NO_DELEG;
            }
            op.Opopen.Share_access = new Uint32T(access);
            op.Opopen.Share_deny = new Uint32T(NFSv4Protocol.OPEN4_SHARE_DENY_NONE);

            Openflag4 flag = new Openflag4();
            flag.Opentype = Opentype4.OPEN4_NOCREATE;

            // Createhow4(mode, attrs, verifier)
            Createhow4 how = new Createhow4();
            how.Mode = Createmode4.UNCHECKED4;
            //how.mode = Createmode4.EXCLUSIVE4_1;
            Fattr4 attr = new Fattr4();

            attr.Attrmask = OpenFattrBitmap();
            attr.Attr_vals = new Attrlist4();
            attr.Attr_vals.Value = OpenAttrs(7, 7, 7, 0);

            how.Createattrs = attr;
            how.Createverf = new Verifier4(0);  //it is long
            //how.mode = Createmode4.GUARDED4;

            flag.How = how;
            op.Opopen.Openhow = flag;

            OpenClaim4 claim = new OpenClaim4();
            claim.Claim = OpenClaimType4.CLAIM_NULL;
            claim.File = new Component4(new Utf8strCs(new Utf8string(encoding
                    .GetBytes(path))));
            claim.Delegate_type = NFSv4Protocol.OPEN4_SHARE_ACCESS_WANT_NO_DELEG;
            claim.File_delegate_prev = null;
            claim.Oc_delegate_stateid = null;
            claim.Delegate_type = 0;
            claim.Delegate_cur_info = null;

            op.Opopen.Claim = claim;

            return op;
        }

        /// <summary>
        /// Creates a Bitmap4 structure with attribute masks for file open operations.
        /// Configures the bitmap to include SIZE and MODE attributes, which are
        /// commonly set when creating or opening files.
        /// </summary>
        /// <returns>A Bitmap4 structure configured for SIZE and MODE attributes.</returns>
        public static Bitmap4 OpenFattrBitmap()
        {
            List<int> attrs = new List<int>();

            //for dir we don't need this
            attrs.Add(NFSv4Protocol.FATTR4_SIZE);
            attrs.Add(NFSv4Protocol.FATTR4_MODE);

            Bitmap4 afttrBitmap = new Bitmap4();
            //changed to 1
            afttrBitmap.Value = new Uint32T[2];
            afttrBitmap.Value[0] = new Uint32T();
            afttrBitmap.Value[1] = new Uint32T();

            foreach (int mask in attrs)
            {
                int bit;
                Uint32T bitmap;
                if (mask > 31)
                {
                    bit = mask - 32;
                    bitmap = afttrBitmap.Value[1];
                }
                else
                {
                    bit = mask;
                    bitmap = afttrBitmap.Value[0];
                }

                bitmap.Value |= 1 << bit;
            }

            return afttrBitmap;
        }

        /// <summary>
        /// Encodes file attributes (mode and size) into a byte array for OPEN operations.
        /// Note: The permission parameters are currently ignored and overridden to 0777 (rwxrwxrwx).
        /// Encodes attributes in XDR format as required by the NFSv4 protocol.
        /// </summary>
        /// <param name="user">User permission bits (0-7) - currently ignored, defaults to 7.</param>
        /// <param name="group">Group permission bits (0-7) - currently ignored, defaults to 7.</param>
        /// <param name="other">Other permission bits (0-7) - currently ignored, defaults to 7.</param>
        /// <param name="sizea">The file size to set (typically 0 for new files).</param>
        /// <returns>A byte array containing the XDR-encoded attributes.</returns>
        public static byte[] OpenAttrs(int user, int group, int other, long sizea)
        {
            XdrBufferEncodingStream xdr = new XdrBufferEncodingStream(1024);

            //starts encoding
            xdr.BeginEncoding(null, 0);

            user = 7 << 6;
            group = 7 << 3;
            other = 7;

            Mode4 fmode = new Mode4();
            fmode.Value = new Uint32T(group + user + other);
            Fattr4Mode mode = new Fattr4Mode(fmode);

            Fattr4Size size = new Fattr4Size(new Uint64T(sizea));

            size.XdrEncode(xdr);
            mode.XdrEncode(xdr);

            xdr.EndEncoding();
            //end encoding

            byte[] retBytes = new byte[xdr.GetXdrLength()];

            Array.Copy(xdr.GetXdrData(), 0, retBytes, 0, xdr.GetXdrLength());

            return retBytes;
        }
    }
}
