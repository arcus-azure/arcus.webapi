using System;
using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Interfaces;

namespace Arcus.WebApi.Unit.Security
{
    /// <summary>
    /// Secret provider to stub out the secret value.
    /// </summary>
    public class StubSecretProvider : ISecretProvider
    {
        private readonly string _secretValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubSecretProvider"/> class.
        /// </summary>
        /// <param name="secretValue">The value of the secret to return.</param>
        public StubSecretProvider(string secretValue)
        {
            _secretValue = secretValue;
        }

        public Task<string> Get(string secretName)
        {
            return Task.FromResult(_secretValue);
        }
    }
}
