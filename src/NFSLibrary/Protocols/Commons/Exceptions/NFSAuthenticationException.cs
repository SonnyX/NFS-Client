namespace NFSLibrary.Protocols.Commons.Exceptions
{
    using System.Security.Authentication;

    /// <summary>
    /// Represents an NFS authentication exception.
    /// </summary>
    public class NFSAuthenticationException : AuthenticationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSAuthenticationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSAuthenticationException(string message)
            : base(message)
        {
        }
    }
}
