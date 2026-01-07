namespace NFSLibrary.Protocols.Commons.Exceptions
{
    using System;

    /// <summary>
    /// Represents an NFS communication exception.
    /// </summary>
    public class NFSCommunicationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSCommunicationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSCommunicationException(string message)
            : base(message)
        {
        }
    }
}
