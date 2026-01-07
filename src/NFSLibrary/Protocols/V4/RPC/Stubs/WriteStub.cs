namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4 WRITE operation requests.
    /// The WRITE operation writes data to a regular file that has been opened with the OPEN operation.
    /// It requires a valid stateid obtained from the OPEN operation and supports writing data
    /// at specific offsets in the file. This implementation uses UNSTABLE4 mode for better performance,
    /// which allows the server to cache writes before committing them to stable storage.
    /// </summary>
    internal class WriteStub
    {
        /// <summary>
        /// Generates a WRITE operation request to write data to a file.
        /// Uses UNSTABLE4 mode by default, which allows server-side caching for better performance.
        /// For guaranteed synchronous writes, change the stable mode to FILE_SYNC4.
        /// </summary>
        /// <param name="offset">The offset in the file to start writing at (in bytes).</param>
        /// <param name="data">The data to write to the file.</param>
        /// <param name="stateid">The state ID of the open file obtained from OPEN operation.</param>
        /// <returns>An NfsArgop4 structure containing the WRITE operation request.</returns>
        public static NfsArgop4 GenerateRequest(long offset, byte[] data, Stateid4 stateid)
        {
            Write4Args args = new Write4Args();

            //enable this for sycronized stable writes
            //args.Stable = StableHow4.FILE_SYNC4;

            args.Stable = StableHow4.UNSTABLE4;

            args.Offset = new Offset4(new Uint64T(offset));

            args.Stateid = stateid;

            args.Data = data;

            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_WRITE;
            op.Opwrite = args;

            return op;
        }
    }
}
