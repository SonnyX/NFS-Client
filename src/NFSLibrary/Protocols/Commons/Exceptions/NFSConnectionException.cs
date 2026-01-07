namespace NFSLibrary.Protocols.Commons.Exceptions
{
    using System;

    /// <summary>
    /// Represents an NFS connection exception.
    /// </summary>
    public class NFSConnectionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSConnectionException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSConnectionException(string message)
            : base(message)
        {
        }
    }
}
