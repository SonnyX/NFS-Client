namespace NFSLibrary.Protocols.Commons.Exceptions
{
    using System;

    /// <summary>
    /// Represents an NFS unauthorized access exception.
    /// </summary>
    public class NFSUnauthorizedAccessException : UnauthorizedAccessException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSUnauthorizedAccessException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSUnauthorizedAccessException(string message)
            : base(message)
        {
        }
    }
}
