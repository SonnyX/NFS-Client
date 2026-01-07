namespace NFSLibrary.Protocols.Commons.Exceptions
{
    using System;

    /// <summary>
    /// Represents a general NFS exception.
    /// </summary>
    public class NFSGeneralException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSGeneralException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSGeneralException(string message)
            : base(message)
        {
        }
    }
}
