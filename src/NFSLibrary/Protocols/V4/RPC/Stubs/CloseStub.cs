namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 CLOSE operation requests.
    /// The CLOSE operation releases the state associated with an open file, releasing locks
    /// and other state information maintained by the server for that open file handle.
    /// This should be called when a client is done with a file opened via the OPEN operation.
    /// </summary>
    internal class CloseStub
    {
        /// <summary>
        /// Generates a CLOSE operation request to close an open file.
        /// </summary>
        /// <param name="stateid">The state ID of the open file to close.</param>
        /// <returns>An NfsArgop4 structure containing the CLOSE operation request.</returns>
        public static NfsArgop4 GenerateRequest(Stateid4 stateid)
        {
            Close4Args args = new Close4Args();

            args.Seqid = new Seqid4(new Uint32T(0));
            args.Open_stateid = stateid;

            NfsArgop4 op = new NfsArgop4();

            op.Argop = NfsOpnum4.OP_CLOSE;
            op.Opclose = args;

            return op;
        }
    }
}
