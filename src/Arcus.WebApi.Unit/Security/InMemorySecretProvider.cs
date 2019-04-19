using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Interfaces;
using GuardNet;

namespace Arcus.WebApi.Unit.Security
{
    /// <summary>
    /// <see cref="ISecretProvider"/> implementation that provides an in-memory storage of secrets by name.
    /// </summary>
    public class InMemorySecretProvider : ISecretProvider
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
        /// <returns>Returns a <see cref="Task"/> that contains the secret key</returns>
        /// <exception cref="ArgumentException">The name must not be empty</exception>
        /// <exception cref="ArgumentNullException">The name must not be null</exception>
        public Task<string> Get(string secretName)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            if (_secretValueByName.TryGetValue(secretName, out string secretValue))
            {
                return Task.FromResult(secretValue);
            }

            return Task.FromResult<string>(null);
        }
    }
}
