namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 READLINK operation requests.
    /// The READLINK operation reads the contents of a symbolic link.
    /// </summary>
    internal class ReadLinkStub
    {
        /// <summary>
        /// Generates a READLINK operation request.
        /// </summary>
        /// <returns>An NfsArgop4 structure containing the READLINK operation request.</returns>
        /// <remarks>
        /// READLINK takes no arguments; it operates on the current file handle
        /// which must be set to a symbolic link via PUTFH before calling READLINK.
        /// </remarks>
        public static NfsArgop4 GenerateRequest()
        {
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_READLINK;
            // READLINK has no arguments - operates on current file handle
            return op;
        }
    }
}
