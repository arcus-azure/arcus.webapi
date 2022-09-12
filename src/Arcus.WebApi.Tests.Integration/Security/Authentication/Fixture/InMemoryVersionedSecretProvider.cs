using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Security.Core;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Fixture
{
    public class InMemoryVersionedSecretProvider : IVersionedSecretProvider
    {
        private readonly Secret[] _secrets;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryVersionedSecretProvider" /> class.
        /// </summary>
        public InMemoryVersionedSecretProvider(params Secret[] secrets)
        {
            _secrets = secrets;
        }

        public Task<string> GetRawSecretAsync(string secretName)
        {
            return Task.FromResult(_secrets[0].Value);
        }

        public Task<Secret> GetSecretAsync(string secretName)
        {
            return Task.FromResult(_secrets[0]);
        }

        public Task<IEnumerable<string>> GetRawSecretsAsync(string secretName, int amountOfVersions)
        {
            return Task.FromResult(_secrets.Take(amountOfVersions).Select(secret => secret.Value));
        }

        public Task<IEnumerable<Secret>> GetSecretsAsync(string secretName, int amountOfVersions)
        {
            return Task.FromResult(_secrets.Take(amountOfVersions));
        }
    }
}
