namespace NFSLibrary.Protocols.Commons.Exceptions.Mount
{
    using System.IO;

    /// <summary>
    /// Represents an NFS mount I/O exception.
    /// </summary>
    public class NFSMountIOException : IOException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSMountIOException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSMountIOException(string message)
            : base(message)
        {
        }
    }
}
