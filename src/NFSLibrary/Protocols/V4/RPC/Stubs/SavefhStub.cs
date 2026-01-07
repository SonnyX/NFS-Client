namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 SAVEFH operation requests.
    /// The SAVEFH operation saves the current file handle to a saved file handle slot,
    /// which can later be restored using RESTOREFH. This is useful for operations that
    /// need to work with two file handles simultaneously (e.g., RENAME, LINK).
    /// </summary>
    internal class SavefhStub
    {
        /// <summary>
        /// Generates a SAVEFH operation request to save the current file handle.
        /// </summary>
        /// <returns>An NfsArgop4 structure containing the SAVEFH operation request.</returns>
        public static NfsArgop4 GenerateRequest()
        {
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_SAVEFH;
            return op;
        }
    }
}
