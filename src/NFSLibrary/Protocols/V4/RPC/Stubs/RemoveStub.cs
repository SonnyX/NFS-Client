namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    using System;

    /// <summary>
    /// Provides stub methods for creating NFSv4 REMOVE operation requests.
    /// The REMOVE operation deletes a file or directory from the current directory.
    /// For directories, the directory must be empty. The current file handle must
    /// refer to the parent directory containing the object to be removed.
    /// </summary>
    internal class RemoveStub
    {
        /// <summary>
        /// Generates a REMOVE operation request to delete a file or directory.
        /// </summary>
        /// <param name="path">The path of the file or directory to remove.</param>
        /// <returns>An NfsArgop4 structure containing the REMOVE operation request.</returns>
        public static NfsArgop4 GenerateRequest(String path)
        {
            Remove4Args args = new Remove4Args();

            args.Target = new Component4();
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            args.Target.Value = new Utf8strCs(new Utf8string(encoding.GetBytes(path)));

            NfsArgop4 op = new NfsArgop4();

            op.Argop = NfsOpnum4.OP_REMOVE;
            op.Opremove = args;

            return op;
        }
    }
}
