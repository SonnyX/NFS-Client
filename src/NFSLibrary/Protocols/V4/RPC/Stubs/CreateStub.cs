namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using System;

    /// <summary>
    /// Provides stub methods for creating NFSv4 CREATE operation requests.
    /// The CREATE operation creates a new non-regular file object (directory, named pipe, etc.)
    /// in the current directory. This implementation specifically creates directories.
    /// For regular files, use the OPEN operation instead.
    /// </summary>
    internal class CreateStub
    {
        /// <summary>
        /// Generates a CREATE operation request to create a new directory.
        /// </summary>
        /// <param name="name">The name of the directory to create.</param>
        /// <param name="attribs">The file attributes for the new directory.</param>
        /// <returns>An NfsArgop4 structure containing the CREATE operation request.</returns>
        public static NfsArgop4 GenerateRequest(String name, Fattr4 attribs)
        {
            NfsArgop4 op = new NfsArgop4();
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            op.Argop = NfsOpnum4.OP_CREATE;
            op.Opcreate = new Create4Args();
            op.Opcreate.Objname = new Component4();
            op.Opcreate.Objname.Value = new Utf8strCs(new Utf8string(encoding.GetBytes(name)));
            op.Opcreate.Createattrs = attribs;
            op.Opcreate.Objtype = new Createtype4();
            //we will create only directories
            op.Opcreate.Objtype.Type = NfsFtype4.NF4DIR;

            return op;
        }
    }
}
