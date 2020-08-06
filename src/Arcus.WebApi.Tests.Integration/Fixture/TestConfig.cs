using System;
using System.Collections.Generic;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a <see cref="IConfiguration"/> implementation that loads the integration configuration values.
    /// </summary>
    public class TestConfig : IConfigurationRoot
    {
        private IConfigurationRoot _config;

        private TestConfig(IConfigurationRoot config)
        {
            Guard.NotNull(config, nameof(config));
            _config = config;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TestConfig"/> with the integration configuration values loaded.
        /// </summary>
        public static TestConfig Create()
        {
            IConfigurationRoot config = 
                new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile("appsettings.local.json", optional: true)
                    .Build();

            return new TestConfig(config);
        }

        /// <summary>
        /// Gets the HTTP port configured in this integration test configuration.
        /// </summary>
        public int GetHttpPort()
        {
            const string key = "Arcus:Infra:HttpPort";

            var httpPort = _config.GetValue<string>(key);
            if (Int32.TryParse(httpPort, out int result) && result > 0)
            {
                return result;
            }

            throw new KeyNotFoundException(
                $"Cannot find '{key}' in integration test configuration that represents a valid HTTP port");
        }

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _config.GetChildren();
        }

        /// <summary>
        /// Returns a <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" /> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>A <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" />.</returns>
        public IChangeToken GetReloadToken()
        {
            return _config.GetReloadToken();
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" />.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _config.GetSection(key);
        }

        /// <summary>Gets or sets a configuration value.</summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key]
        {
            get => _config[key];
            set => _config[key] = value;
        }

        /// <summary>
        /// Force the configuration values to be reloaded from the underlying <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s.
        /// </summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>
        /// The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s for this configuration.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers => _config.Providers;
    }
}
