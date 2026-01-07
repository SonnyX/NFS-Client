namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 ACCESS operation requests.
    /// The ACCESS operation checks the access permissions for the current file handle,
    /// allowing clients to verify whether they have specific types of access (read, write, execute)
    /// before attempting file operations.
    /// </summary>
    internal class AcessStub
    {
        /// <summary>
        /// Generates an ACCESS operation request with the specified access arguments.
        /// </summary>
        /// <param name="acessargs">The access arguments specifying the access mode.</param>
        /// <returns>An NfsArgop4 structure containing the ACCESS operation request.</returns>
        public static NfsArgop4 GenerateRequest(Uint32T acessargs)
        {
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_ACCESS;

            op.Opaccess = new Access4Args();
            op.Opaccess.Access = acessargs;

            return op;
        }
    }
}
