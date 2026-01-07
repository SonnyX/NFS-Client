namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using System;

    /// <summary>
    /// Provides stub methods for creating NFSv4 RENAME operation requests.
    /// The RENAME operation renames a file or directory from the old name to the new name.
    /// Both names are relative to the saved directory (set via SAVEFH) for the source and
    /// the current directory for the destination. Typically used in a compound with SAVEFH
    /// and PUTFH operations to handle cross-directory renames.
    /// </summary>
    internal class RenameStub
    {
        /// <summary>
        /// Generates a RENAME operation request to rename a file or directory.
        /// </summary>
        /// <param name="oldName">The current name of the file or directory.</param>
        /// <param name="newName">The new name for the file or directory.</param>
        /// <returns>An NfsArgop4 structure containing the RENAME operation request.</returns>
        public static NfsArgop4 GenerateRequest(String oldName, String newName)
        {
            Rename4Args args = new Rename4Args();

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

            args.Oldname = new Component4();
            args.Oldname.Value = new Utf8strCs(new Utf8string(encoding.GetBytes(oldName)));

            args.Newname = new Component4();
            args.Newname.Value = new Utf8strCs(new Utf8string(encoding.GetBytes(newName)));

            NfsArgop4 op = new NfsArgop4();

            op.Argop = NfsOpnum4.OP_RENAME;
            op.Oprename = args;

            return op;
        }
    }
}
