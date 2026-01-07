namespace NFSLibrary.Protocols.Commons.Exceptions.Mount
{
    using System;

    /// <summary>
    /// Represents a general NFS mount exception.
    /// </summary>
    public class NFSMountGeneralException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSMountGeneralException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSMountGeneralException(string message)
            : base(message)
        {
        }
    }
}
