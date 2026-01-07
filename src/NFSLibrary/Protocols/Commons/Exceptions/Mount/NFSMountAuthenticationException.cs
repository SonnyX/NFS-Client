namespace NFSLibrary.Protocols.Commons.Exceptions.Mount
{
    using System.Security.Authentication;

    /// <summary>
    /// Represents an NFS mount authentication exception.
    /// </summary>
    public class NFSMountAuthenticationException : AuthenticationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NFSMountAuthenticationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NFSMountAuthenticationException(string message)
            : base(message)
        {
        }
    }
}
