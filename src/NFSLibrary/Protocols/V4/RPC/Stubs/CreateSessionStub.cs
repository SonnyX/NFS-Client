namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using System;

    /// <summary>
    /// Provides stub methods for creating NFSv4.1 CREATE_SESSION operation requests.
    /// The CREATE_SESSION operation establishes a session between the client and server,
    /// which is required for NFSv4.1 operations. Sessions provide connection association,
    /// exactly-once semantics, and support for callbacks. This must be called after EXCHANGE_ID
    /// and before performing file operations in NFSv4.1.
    /// </summary>
    internal class CreateSessionStub
    {
        /// <summary>
        /// Generates a standard CREATE_SESSION operation request for establishing a session.
        /// Configures both fore channel (client-to-server) and back channel (server-to-client)
        /// attributes with reasonable defaults for typical NFS operations.
        /// </summary>
        /// <param name="eir_clientid">The client ID obtained from EXCHANGE_ID response.</param>
        /// <param name="eir_sequenceid">The sequence ID obtained from EXCHANGE_ID response.</param>
        /// <returns>An NfsArgop4 structure containing the CREATE_SESSION operation request.</returns>
        public static NfsArgop4 Standard(Clientid4 eir_clientid,
                Sequenceid4 eir_sequenceid)
        {
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_CREATE_SESSION;
            op.Opcreate_session = new CreateSession4Args();
            ChannelAttrs4 chan_attrs = new ChannelAttrs4();

            chan_attrs.Ca_headerpadsize = new Count4(new Uint32T(0));
            chan_attrs.Ca_maxoperations = new Count4(new Uint32T(8));
            chan_attrs.Ca_maxrequests = new Count4(new Uint32T(128));
            chan_attrs.Ca_maxrequestsize = new Count4(new Uint32T(1049620));
            chan_attrs.Ca_maxresponsesize = new Count4(new Uint32T(1049480));
            chan_attrs.Ca_maxresponsesize_cached = new Count4(new Uint32T(2868));
            chan_attrs.Ca_rdma_ird = new Uint32T[0];

            op.Opcreate_session.Csa_clientid = eir_clientid;
            op.Opcreate_session.Csa_sequence = eir_sequenceid;
            //connection back channel
            op.Opcreate_session.Csa_flags = new Uint32T(0);  //3 if u want to use the back channel
            op.Opcreate_session.Csa_fore_chan_attrs = chan_attrs;

            //diferent chan attrs for fore channel
            ChannelAttrs4 back_chan_attrs = new ChannelAttrs4();
            back_chan_attrs.Ca_headerpadsize = new Count4(new Uint32T(0));
            back_chan_attrs.Ca_maxoperations = new Count4(new Uint32T(2));
            back_chan_attrs.Ca_maxrequests = new Count4(new Uint32T(1));
            back_chan_attrs.Ca_maxrequestsize = new Count4(new Uint32T(4096));
            back_chan_attrs.Ca_maxresponsesize = new Count4(new Uint32T(4096));
            back_chan_attrs.Ca_maxresponsesize_cached = new Count4(new Uint32T(0));
            back_chan_attrs.Ca_rdma_ird = new Uint32T[0];

            op.Opcreate_session.Csa_back_chan_attrs = back_chan_attrs;
            op.Opcreate_session.Csa_cb_program = new Uint32T(0x40000000);

            CallbackSecParms4[] cb = new CallbackSecParms4[1];
            CallbackSecParms4 callb = new CallbackSecParms4();

            //new auth_sys params
            callb.Cb_secflavor = AuthFlavor.AUTH_SYS;
            callb.Cbsp_sys_cred = new AuthsysParms();
            Random r = new Random();
            callb.Cbsp_sys_cred.Stamp = r.Next(); //just random number
            callb.Cbsp_sys_cred.Gid = 0; //maybe root ?
            callb.Cbsp_sys_cred.Uid = 0; //maybe root ?

            callb.Cbsp_sys_cred.Machinename = System.Environment.MachineName;
            callb.Cbsp_sys_cred.Gids = new int[0];

            //callb.Cb_secflavor = AuthFlavor.AUTH_NONE;

            cb[0] = callb;
            // op.Opcreate_session.csa_sec_parms = new CallbackSecParms4[1];
            op.Opcreate_session.csa_sec_parms = cb;
            return op;
        }
    }
}
