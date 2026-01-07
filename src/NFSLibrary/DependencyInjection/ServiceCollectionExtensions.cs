#if NET8_0_OR_GREATER
namespace NFSLibrary.DependencyInjection
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// Extension methods for configuring NFS services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds NFS client services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for NFS options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNfsClient(
            this IServiceCollection services,
            Action<NfsClientOptions>? configure = null)
        {
            NfsClientOptions options = new NfsClientOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);

            // Register the client factory
            services.TryAddSingleton<INfsClientFactory, NfsClientFactory>();

            // Register a scoped client that uses the factory
            services.TryAddScoped<NfsClient>(sp =>
            {
                INfsClientFactory factory = sp.GetRequiredService<INfsClientFactory>();
                NfsClientOptions opts = sp.GetRequiredService<NfsClientOptions>();
                return factory.CreateClient(opts);
            });

            return services;
        }

        /// <summary>
        /// Adds NFS client services with a named configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the configuration.</param>
        /// <param name="configure">Configuration action for NFS options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNfsClient(
            this IServiceCollection services,
            string name,
            Action<NfsClientOptions> configure)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            NfsClientOptions options = new NfsClientOptions { Name = name };
            configure(options);

            // Get or create options collection
            NfsClientOptionsCollection? optionsCollection = services.FirstOrDefault(s => s.ServiceType == typeof(NfsClientOptionsCollection))
                ?.ImplementationInstance as NfsClientOptionsCollection;

            if (optionsCollection == null)
            {
                optionsCollection = new NfsClientOptionsCollection();
                services.TryAddSingleton(optionsCollection);
            }

            optionsCollection.AddOptions(name, options);
            services.TryAddSingleton<INfsClientFactory, NfsClientFactory>();

            return services;
        }

        /// <summary>
        /// Adds NFS connection pooling services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for pool options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNfsConnectionPool(
            this IServiceCollection services,
            Action<NfsConnectionPoolOptions>? configure = null)
        {
            NfsConnectionPoolOptions options = new NfsConnectionPoolOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);
            services.TryAddSingleton<NfsConnectionPool>();

            return services;
        }

        /// <summary>
        /// Adds NFS connection health monitoring services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for health options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNfsHealthChecks(
            this IServiceCollection services,
            Action<NfsConnectionHealthOptions>? configure = null)
        {
            NfsConnectionHealthOptions options = new NfsConnectionHealthOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);

            return services;
        }
    }
}
#endif
