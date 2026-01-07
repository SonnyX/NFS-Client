namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using System;

    /// <summary>
    /// Provides stub methods for creating NFSv4 LINK operation requests.
    /// The LINK operation creates a hard link from the saved file handle (set via SAVEFH)
    /// to a new name in the current directory (set via PUTFH).
    /// </summary>
    internal class LinkStub
    {
        /// <summary>
        /// Generates a LINK operation request to create a hard link.
        /// </summary>
        /// <param name="newName">The name for the new hard link in the current directory.</param>
        /// <returns>An NfsArgop4 structure containing the LINK operation request.</returns>
        /// <remarks>
        /// Before calling LINK, the compound must include:
        /// 1. PUTFH to set the source file as current file handle
        /// 2. SAVEFH to save the source file handle
        /// 3. PUTFH to set the destination directory as current file handle
        /// 4. LINK with the new name
        /// </remarks>
        public static NfsArgop4 GenerateRequest(string newName)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_LINK;
            op.Oplink = new Link4Args();
            op.Oplink.Newname = new Component4();
            op.Oplink.Newname.Value = new Utf8strCs(
                new Utf8string(encoding.GetBytes(newName)));

            return op;
        }
    }
}
