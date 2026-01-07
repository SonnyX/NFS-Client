namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 READ operation requests.
    /// The READ operation reads data from a regular file that has been opened with the OPEN operation.
    /// It requires a valid stateid obtained from the OPEN operation and supports reading data
    /// at specific offsets in the file.
    /// </summary>
    internal class ReadStub
    {
        /// <summary>
        /// Generates a READ operation request to read data from a file.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="offset">The offset in the file to start reading from.</param>
        /// <param name="stateid">The state ID of the open file.</param>
        /// <returns>An NfsArgop4 structure containing the READ operation request.</returns>
        public static NfsArgop4 GenerateRequest(int count, long offset, Stateid4 stateid)
        {
            Read4Args args = new Read4Args();
            args.Count = new Count4(new Uint32T(count));
            args.Offset = new Offset4(new Uint64T(offset));

            args.Stateid = stateid;

            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_READ;
            op.Opread = args;

            return op;
        }
    }
}
