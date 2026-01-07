namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using System;

    /// <summary>
    /// Provides stub methods for creating NFSv4 symbolic link creation requests.
    /// Uses the CREATE operation with NF4LNK file type to create symbolic links.
    /// </summary>
    internal class SymlinkStub
    {
        /// <summary>
        /// Generates a CREATE operation request to create a symbolic link.
        /// </summary>
        /// <param name="linkName">The name of the symbolic link to create.</param>
        /// <param name="targetPath">The target path the symbolic link will point to.</param>
        /// <param name="attribs">The file attributes for the symbolic link.</param>
        /// <returns>An NfsArgop4 structure containing the CREATE operation request.</returns>
        public static NfsArgop4 GenerateRequest(string linkName, string targetPath, Fattr4 attribs)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_CREATE;
            op.Opcreate = new Create4Args();

            // Set the name of the symbolic link
            op.Opcreate.Objname = new Component4();
            op.Opcreate.Objname.Value = new Utf8strCs(
                new Utf8string(encoding.GetBytes(linkName)));

            // Set attributes
            op.Opcreate.Createattrs = attribs;

            // Set type to symbolic link with target path
            op.Opcreate.Objtype = new Createtype4();
            op.Opcreate.Objtype.Type = NfsFtype4.NF4LNK;
            op.Opcreate.Objtype.Linkdata = new Linktext4();
            op.Opcreate.Objtype.Linkdata.Value = new Utf8strCs(
                new Utf8string(encoding.GetBytes(targetPath)));

            return op;
        }
    }
}
