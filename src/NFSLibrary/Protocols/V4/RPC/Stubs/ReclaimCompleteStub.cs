namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4.1 RECLAIM_COMPLETE operation requests.
    /// The RECLAIM_COMPLETE operation indicates that the client has finished reclaiming
    /// state after a server restart or network partition. This is required in NFSv4.1
    /// before the client can perform non-reclaim operations. It should be sent once per
    /// client ID after CREATE_SESSION.
    /// </summary>
    internal class ReclaimCompleteStub
    {
        /// <summary>
        /// Generates a RECLAIM_COMPLETE operation request to indicate completion of state reclaim.
        /// Typically called with false to indicate all filesystems are complete.
        /// </summary>
        /// <param name="RcaOneFs2">If true, reclaim is complete for one filesystem; if false, for all filesystems.</param>
        /// <returns>An NfsArgop4 structure containing the RECLAIM_COMPLETE operation request.</returns>
        public static NfsArgop4 GenerateRequest(bool RcaOneFs2)
        {
            NfsArgop4 op = new NfsArgop4();

            op.Argop = NfsOpnum4.OP_RECLAIM_COMPLETE;
            op.Opreclaim_complete = new ReclaimComplete4Args();
            op.Opreclaim_complete.RcaOneFs = RcaOneFs2;

            return op;
        }
    }
}
