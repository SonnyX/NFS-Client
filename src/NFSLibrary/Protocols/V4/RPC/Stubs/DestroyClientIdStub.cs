namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4.1 DESTROY_CLIENTID operation requests.
    /// The DESTROY_CLIENTID operation destroys a client ID that was created via EXCHANGE_ID,
    /// releasing all state associated with that client. This should only be called when
    /// the client has no active sessions or state on the server.
    /// </summary>
    internal class DestroyClientIdStub
    {
        /// <summary>
        /// Generates a standard DESTROY_CLIENTID operation request to destroy a client ID.
        /// </summary>
        /// <param name="client">The client ID to destroy.</param>
        /// <returns>An NfsArgop4 structure containing the DESTROY_CLIENTID operation request.</returns>
        public static NfsArgop4 Standard(Clientid4 client)
        {
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_DESTROY_CLIENTID;
            op.Opdestroy_clientid = new DestroyClientid4Args();

            op.Opdestroy_clientid.Dca_clientid = client;

            return op;
        }
    }
}
