namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using System;

    /// <summary>
    /// Provides stub methods for creating NFSv4 LOOKUP operation requests.
    /// The LOOKUP operation traverses one component of a pathname, changing the current
    /// file handle to the named object within the current directory. Multiple LOOKUP
    /// operations are typically chained together to resolve full pathnames.
    /// </summary>
    internal class LookupStub
    {
        /// <summary>
        /// Generates a LOOKUP operation request to resolve a path component.
        /// This should be a single path component (directory or file name), not a full path.
        /// </summary>
        /// <param name="path">The path component to look up (e.g., "mydir" or "file.txt").</param>
        /// <returns>An NfsArgop4 structure containing the LOOKUP operation request.</returns>
        public static NfsArgop4 GenerateRequest(String path)
        {
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_LOOKUP;
            op.Oplookup = new Lookup4Args();

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] bytes = encoding.GetBytes(path);

            op.Oplookup.Objname = new Component4(new Utf8strCs(new Utf8string(bytes)));

            return op;
        }
    }
}
