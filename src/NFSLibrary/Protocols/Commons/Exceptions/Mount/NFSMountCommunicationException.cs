namespace NFSLibrary.Protocols.Commons.Exceptions.Mount
{
    using System;

    /// <summary>
    /// Represents an NFS mount communication exception.
    /// </summary>
    public class NFSMountCommunicationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSMountCommunicationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSMountCommunicationException(string message)
            : base(message)
        {
        }
    }
}
