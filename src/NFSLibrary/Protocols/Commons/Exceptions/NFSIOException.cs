namespace NFSLibrary.Protocols.Commons.Exceptions
{
    using System.IO;

    /// <summary>
    /// Represents an NFS I/O exception.
    /// </summary>
    public class NFSIOException : IOException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSIOException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSIOException(string message)
            : base(message)
        {
        }
    }
}
