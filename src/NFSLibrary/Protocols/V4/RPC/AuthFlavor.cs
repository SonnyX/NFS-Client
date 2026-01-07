namespace NFSLibrary.Protocols.V4.RPC
{
    /// <summary>
    /// Defines authentication flavor constants for RPC authentication.
    /// </summary>
    public class AuthFlavor
    {
        /// <summary>
        /// No authentication.
        /// </summary>
        public const int AUTH_NONE = 0;

        /// <summary>
        /// UNIX style authentication.
        /// </summary>
        public const int AUTH_SYS = 1;

        /// <summary>
        /// Short hand UNIX style authentication.
        /// </summary>
        public const int AUTH_SHORT = 2;

        /// <summary>
        /// RPCSEC_GSS authentication.
        /// </summary>
        public const int RPCSEC_GSS = 6;
    }
}
