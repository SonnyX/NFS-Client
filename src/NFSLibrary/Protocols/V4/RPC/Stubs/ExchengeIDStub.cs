namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using System;

    /// <summary>
    /// Provides stub methods for creating NFSv4.1 EXCHANGE_ID operation requests.
    /// The EXCHANGE_ID operation establishes a client ID on the server, which is the first
    /// step in establishing NFSv4.1 session semantics. It exchanges client and server
    /// implementation information and creates a client identifier that will be used in
    /// subsequent CREATE_SESSION operations.
    /// </summary>
    internal class ExchengeIDStub
    {
        /// <summary>
        /// Generates a normal EXCHANGE_ID operation request for client identification.
        /// This creates a client identifier using the current machine name and timestamp-based
        /// verifier to ensure uniqueness across client restarts.
        /// </summary>
        /// <param name="nii_domain">The implementation domain name (e.g., organization domain).</param>
        /// <param name="nii_name">The implementation name (e.g., application or library name).</param>
        /// <param name="Co_ownerid">The client owner ID (unique identifier for this client instance).</param>
        /// <param name="flags">The exchange ID flags controlling session behavior.</param>
        /// <param name="how">The state protection mode (e.g., AUTH_SYS, RPCSEC_GSS).</param>
        /// <returns>An NfsArgop4 structure containing the EXCHANGE_ID operation request.</returns>
        public static NfsArgop4 Normal(string nii_domain, string nii_name,
        string Co_ownerid, int flags, int how)
        {
            //for transormation
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_EXCHANGE_ID;
            op.Opexchange_id = new ExchangeId4Args();

            op.Opexchange_id.Eia_client_impl_id = new NfsImplId4[1];
            NfsImplId4 n4 = new NfsImplId4();
            n4.Nii_domain = new Utf8strCis(new Utf8string(encoding.GetBytes(nii_domain)));
            n4.Nii_name = new Utf8strCs(new Utf8string(encoding.GetBytes(nii_name)));
            op.Opexchange_id.Eia_client_impl_id[0] = n4;

            Nfstime4 releaseDate = new Nfstime4();
            releaseDate.Nseconds = new Uint32T(0);
            releaseDate.Seconds = new Int64T((long)(DateTime.UtcNow - new DateTime
    (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);  //seconds here

            op.Opexchange_id.Eia_client_impl_id[0].Nii_date = releaseDate;
            op.Opexchange_id.Eia_clientowner = new ClientOwner4();

            op.Opexchange_id.Eia_clientowner.Co_ownerid = encoding.GetBytes(Co_ownerid);

            op.Opexchange_id.Eia_clientowner.Co_verifier = new Verifier4();
            op.Opexchange_id.Eia_clientowner.Co_verifier.Value = releaseDate.Seconds.Value;   //new byte[NFSv4Protocol.NFS4_VERIFIER_SIZE];

            //byte[] locVerifier = encoding.GetBytes(releaseDate.Seconds.Value.ToString("X"));

            //int len = locVerifier.Length > NFSv4Protocol.NFS4_VERIFIER_SIZE ? NFSv4Protocol.NFS4_VERIFIER_SIZE : locVerifier.Length;
            // Array.Copy(locVerifier, 0, op.Opexchange_id.Eia_clientowner.Co_verifier.Value, 0, len);

            op.Opexchange_id.Eia_flags = new Uint32T(flags);
            op.Opexchange_id.Eia_state_protect = new StateProtect4A();
            op.Opexchange_id.Eia_state_protect.Spa_how = how;
            return op;
        }
    }
}
