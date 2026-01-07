#if NET8_0_OR_GREATER
namespace NFSLibrary.DependencyInjection
{
    using System.Collections.Generic;

    /// <summary>
    /// Collection of named NFS client options.
    /// </summary>
    public class NfsClientOptionsCollection
    {
        private readonly Dictionary<string, NfsClientOptions> _options = new();

        /// <summary>
        /// Adds options with the specified name.
        /// </summary>
        /// <param name="name">The name of the configuration.</param>
        /// <param name="options">The options to add.</param>
        public void AddOptions(string name, NfsClientOptions options)
        {
            _options[name] = options;
        }

        /// <summary>
        /// Gets options by name.
        /// </summary>
        /// <param name="name">The name of the configuration.</param>
        /// <returns>The options if found, or null.</returns>
        public NfsClientOptions? GetOptions(string name)
        {
            return _options.TryGetValue(name, out NfsClientOptions? options) ? options : null;
        }
    }
}
#endif
