#if NET8_0_OR_GREATER
namespace NFSLibrary.DependencyInjection
{
    /// <summary>
    /// Factory for creating NFS clients.
    /// </summary>
    public interface INfsClientFactory
    {
        /// <summary>
        /// Creates a new NFS client with the specified options.
        /// </summary>
        /// <param name="options">The options for the client.</param>
        /// <returns>A new NFS client instance.</returns>
        NfsClient CreateClient(NfsClientOptions options);

        /// <summary>
        /// Creates a named NFS client.
        /// </summary>
        /// <param name="name">The name of the client configuration.</param>
        /// <returns>A new NFS client instance.</returns>
        NfsClient CreateClient(string name);
    }
}
#endif
