namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 PUTROOTFH operation requests.
    /// The PUTROOTFH operation sets the current file handle to the root of the filesystem
    /// pseudo-tree on the server. This is typically the first operation in a compound request
    /// when beginning to navigate the filesystem from the root.
    /// </summary>
    internal class PutrootfhStub
    {
        /// <summary>
        /// Generates a PUTROOTFH operation request to set the current file handle to the root.
        /// </summary>
        /// <returns>An NfsArgop4 structure containing the PUTROOTFH operation request.</returns>
        public static NfsArgop4 GenerateRequest()
        {
            NfsArgop4 op = new NfsArgop4();

            op.Argop = NfsOpnum4.OP_PUTROOTFH;

            return op;
        }
    }
}
