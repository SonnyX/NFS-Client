namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 PUTFH operation requests.
    /// The PUTFH operation sets the current file handle to a previously obtained file handle,
    /// allowing subsequent operations to work on that file or directory. This is commonly used
    /// to restore a saved file handle context when navigating the filesystem.
    /// </summary>
    internal class PutfhStub
    {
        /// <summary>
        /// Generates a PUTFH operation request to set the current file handle.
        /// </summary>
        /// <param name="fh">The file handle to set as current.</param>
        /// <returns>An NfsArgop4 structure containing the PUTFH operation request.</returns>
        public static NfsArgop4 GenerateRequest(NfsFh4 fh)
        {
            NfsArgop4 op = new NfsArgop4();
            op.Opputfh = new Putfh4Args();
            op.Opputfh.Object1 = fh;

            op.Argop = NfsOpnum4.OP_PUTFH;

            return op;
        }
    }
}
