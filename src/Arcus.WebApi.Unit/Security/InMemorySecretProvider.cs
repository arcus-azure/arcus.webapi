using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Security.Core.Caching.Configuration;
using GuardNet;

namespace Arcus.WebApi.Unit.Security
{
    /// <summary>
    /// <see cref="ISecretProvider"/> implementation that provides an in-memory storage of secrets by name.
    /// </summary>
    public class InMemorySecretProvider : ICachedSecretProvider
    {
        private readonly IDictionary<string, string> _secretValueByName;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySecretProvider"/> class.
        /// </summary>
        /// <param name="secretValueByName">The sequence of combinations of secret names and values.</param>
        public InMemorySecretProvider(params (string name, string value)[] secretValueByName)
        {
            Guard.NotNull(secretValueByName, "Secret name/value combinations cannot be 'null'");

            _secretValueByName = secretValueByName.ToDictionary(t => t.name, t => t.value);
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns the secret key.</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<string> GetRawSecretAsync(string secretName)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            if (_secretValueByName.TryGetValue(secretName, out string secretValue))
            {
                return Task.FromResult(secretValue);
            }

            return Task.FromResult<string>(null);
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns a <see cref="T:Arcus.Security.Core.Secret" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public async Task<Secret> GetSecretAsync(string secretName)
        {
            string secret = await GetRawSecretAsync(secretName);
            return new Secret(secret, version: "0.0.0");
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <param name="ignoreCache">Indicates if the cache should be used or skipped</param>
        /// <returns>Returns a <see cref="T:System.Threading.Tasks.Task`1" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The name must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The name must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public async Task<string> GetRawSecretAsync(string secretName, bool ignoreCache)
        {
            string secret = await GetRawSecretAsync(secretName);
            return secret;
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <param name="ignoreCache">Indicates if the cache should be used or skipped</param>
        /// <returns>Returns a <see cref="T:System.Threading.Tasks.Task`1" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The name must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The name must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public async Task<Secret> GetSecretAsync(string secretName, bool ignoreCache)
        {
            Secret secret = await GetSecretAsync(secretName);
            return secret;
        }

        /// <summary>Gets the cache-configuration for this instance.</summary>
        public ICacheConfiguration Configuration => throw new NotImplementedException();
    }
}
