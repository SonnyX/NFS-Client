namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4.1 DESTROY_SESSION operation requests.
    /// The DESTROY_SESSION operation terminates a session previously created with CREATE_SESSION,
    /// releasing all resources and state associated with that session. This should be called
    /// when disconnecting from the server or when the session is no longer needed.
    /// </summary>
    internal class DestroySessionStub
    {
        /// <summary>
        /// Generates a standard DESTROY_SESSION operation request to terminate a session.
        /// </summary>
        /// <param name="sessionid">The session ID to destroy.</param>
        /// <returns>An NfsArgop4 structure containing the DESTROY_SESSION operation request.</returns>
        public static NfsArgop4 Standard(Sessionid4 sessionid)
        {
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_DESTROY_SESSION;
            op.Opdestroy_session = new DestroySession4Args();

            op.Opdestroy_session.Dsa_sessionid = sessionid;

            return op;
        }
    }
}
