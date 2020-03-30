using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Security.Core.Caching.Configuration;
using GuardNet;

namespace Arcus.WebApi.Tests.Unit.Security
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

        /// <summary>
        /// Retrieves the secret value, based on the given name
        /// </summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <param name="ignoreCache">Indicates if the cache should be used or skipped</param>
        /// <returns>Returns a <see cref="Task{TResult}"/> that contains the secret key</returns>
        /// <exception cref="ArgumentException">The name must not be empty</exception>
        /// <exception cref="ArgumentNullException">The name must not be null</exception>
        public Task<string> GetRawSecretAsync(string secretName, bool ignoreCache)
        {
            return GetRawSecretAsync(secretName);
        }

        /// <summary>
        /// Retrieves the secret value, based on the given name
        /// </summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns a <see cref="Task"/> that contains the secret key</returns>
        /// <exception cref="ArgumentException">The name must not be empty</exception>
        /// <exception cref="ArgumentNullException">The name must not be null</exception>
        public Task<string> GetRawSecretAsync(string secretName)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            if (_secretValueByName.TryGetValue(secretName, out string secretValue))
            {
                return Task.FromResult(secretValue);
            }

            return Task.FromResult<string>(null);
        }

        public Task<string> Get(string secretName)
        {
            return GetRawSecretAsync(secretName);
        }

        public Task<string> Get(string secretName, bool ignoreCache)
        {
            return GetRawSecretAsync(secretName, ignoreCache);
        }

        public Task<Secret> GetSecretAsync(string secretName)
        {
            return GetSecretAsync(secretName, false);
        }

        public async Task<Secret> GetSecretAsync(string secretName, bool ignoreCache)
        {
            var rawSecret = await GetRawSecretAsync(secretName, ignoreCache);

            return new Secret(rawSecret, "v1.0");
        }

        public Task InvalidateSecretAsync(string secretName)
        {
            return Task.CompletedTask;
        }

        public ICacheConfiguration Configuration { get; }
    }
}
