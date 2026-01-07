namespace NFSLibrary.Protocols.Commons.Exceptions.Mount
{
    using System;

    /// <summary>
    /// Represents an NFS mount unauthorized access exception.
    /// </summary>
    public class NFSMountUnauthorizedAccessException : UnauthorizedAccessException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSMountUnauthorizedAccessException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSMountUnauthorizedAccessException(string message)
            : base(message)
        {
        }
    }
}
