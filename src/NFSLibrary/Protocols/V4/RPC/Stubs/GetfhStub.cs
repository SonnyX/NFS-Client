namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 GETFH operation requests.
    /// The GETFH operation retrieves the current file handle after a LOOKUP or other
    /// operation that changes the current file handle context. File handles are used
    /// to identify files and directories in subsequent operations like READ, WRITE, and GETATTR.
    /// </summary>
    internal class GetfhStub
    {
        /// <summary>
        /// Generates a GETFH operation request to retrieve the current file handle.
        /// </summary>
        /// <returns>An NfsArgop4 structure containing the GETFH operation request.</returns>
        public static NfsArgop4 GenerateRequest()
        {
            NfsArgop4 op = new NfsArgop4();

            op.Argop = NfsOpnum4.OP_GETFH;

            return op;
        }
    }
}
