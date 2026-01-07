namespace NFSLibrary.Protocols.Commons.Exceptions.Mount
{
    using System;

    /// <summary>
    /// Represents an NFS mount connection exception.
    /// </summary>
    public class NFSMountConnectionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSMountConnectionException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSMountConnectionException(string message)
            : base(message)
        {
        }
    }
}
