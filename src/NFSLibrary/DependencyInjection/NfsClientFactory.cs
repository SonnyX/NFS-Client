#if NET8_0_OR_GREATER
namespace NFSLibrary.DependencyInjection
{
    using System;
    using System.Net;

    /// <summary>
    /// Default implementation of <see cref="INfsClientFactory"/>.
    /// </summary>
    internal class NfsClientFactory : INfsClientFactory
    {
        private readonly NfsClientOptionsCollection? _optionsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="NfsClientFactory"/> class.
        /// </summary>
        /// <param name="optionsCollection">The collection of named client options.</param>
        public NfsClientFactory(NfsClientOptionsCollection? optionsCollection = null)
        {
            _optionsCollection = optionsCollection;
        }

        /// <inheritdoc />
        public NfsClient CreateClient(NfsClientOptions options)
        {
            if (string.IsNullOrEmpty(options.ServerAddress))
                throw new InvalidOperationException("ServerAddress must be configured");

            NfsClient client = new NfsClient(options.Version);

            if (IPAddress.TryParse(options.ServerAddress, out IPAddress? ipAddress))
            {
                client.Connect(
                    ipAddress,
                    options.UserId,
                    options.GroupId,
                    options.TimeoutMs,
                    System.Text.Encoding.UTF8,
                    options.UseSecurePort,
                    options.UseFhCache);
            }
            else
            {
                // Resolve hostname
                IPAddress[] addresses = Dns.GetHostAddresses(options.ServerAddress);
                if (addresses.Length == 0)
                    throw new InvalidOperationException($"Could not resolve hostname: {options.ServerAddress}");

                client.Connect(
                    addresses[0],
                    options.UserId,
                    options.GroupId,
                    options.TimeoutMs,
                    System.Text.Encoding.UTF8,
                    options.UseSecurePort,
                    options.UseFhCache);
            }

            if (!string.IsNullOrEmpty(options.DefaultExport))
            {
                client.MountDevice(options.DefaultExport);
            }

            return client;
        }

        /// <inheritdoc />
        public NfsClient CreateClient(string name)
        {
            NfsClientOptions options = _optionsCollection?.GetOptions(name)
                ?? throw new InvalidOperationException($"No NFS client configuration found with name: {name}");

            return CreateClient(options);
        }
    }
}
#endif
