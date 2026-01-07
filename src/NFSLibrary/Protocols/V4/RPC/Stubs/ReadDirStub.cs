namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 READDIR operation requests.
    /// The READDIR operation reads directory entries from the current directory file handle.
    /// It supports pagination using cookies and verifiers to handle large directories
    /// that cannot be returned in a single response.
    /// </summary>
    internal class ReadDirStub
    {
        /// <summary>
        /// Generates a READDIR operation request to read directory entries.
        /// Uses a cookie of 0 to start from the beginning, or a previous cookie to continue.
        /// Configured to retrieve up to 10000 bytes of directory entries.
        /// </summary>
        /// <param name="cookie">The cookie for resuming directory reads (0 to start from beginning).</param>
        /// <param name="verifier">The cookie verifier to validate directory hasn't changed between calls.</param>
        /// <returns>An NfsArgop4 structure containing the READDIR operation request.</returns>
        public static NfsArgop4 GenerateRequest(long cookie, Verifier4 verifier)
        {
            NfsArgop4 op = new NfsArgop4();
            op.Opreaddir = new Readdir4Args();
            op.Opreaddir.Cookie = new NfsCookie4(new Uint64T(cookie));
            op.Opreaddir.Dircount = new Count4(new Uint32T(10000));
            op.Opreaddir.Maxcount = new Count4(new Uint32T(10000));
            op.Opreaddir.AttrRequest = new Bitmap4(new Uint32T[] { new Uint32T(0), new Uint32T(0) });
            op.Opreaddir.Cookieverf = verifier;

            op.Argop = NfsOpnum4.OP_READDIR;

            return op;
        }
    }
}
