using System;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Azure.KeyVault;

namespace Arcus.WebApi.Integration.Jobs 
{
    /// <summary>
    /// Representation of a Azure Key Vault secret with a lifetime the same as the type (dispose type = delete secret).
    /// </summary>
    public class TemporaryAzureKeyVaultSecret : IAsyncDisposable
    {
        private readonly IKeyVaultClient _client;
        private readonly string _keyVaultUri;

        private TemporaryAzureKeyVaultSecret(IKeyVaultClient client, string keyVaultUri, string secretName)
        {
            Guard.NotNull(client, nameof(client));
            Guard.NotNullOrWhitespace(keyVaultUri, nameof(keyVaultUri));
            Guard.NotNullOrWhitespace(secretName, nameof(secretName));

            _client = client;
            _keyVaultUri = keyVaultUri;
            
            Name = secretName;
        }

        /// <summary>
        /// Gets the name of the KeyVault secret.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a temporary Azure Key Vault secret, deleting when the <see cref="DisposeAsync"/> is called.
        /// </summary>
        /// <param name="client">The client to the vault where the temporary secret should be set.</param>
        /// <param name="keyVaultUri">The URI of the vault.</param>
        public static async Task<TemporaryAzureKeyVaultSecret> CreateNewAsync(IKeyVaultClient client, string keyVaultUri)
        {
            Guard.NotNull(client, nameof(client));
            Guard.NotNullOrWhitespace(keyVaultUri, nameof(keyVaultUri));

            var testSecretName = Guid.NewGuid().ToString("N");
            var testSecretValue = Guid.NewGuid().ToString("N");
            await client.SetSecretAsync(keyVaultUri, testSecretName, testSecretValue);

            return new TemporaryAzureKeyVaultSecret(client, keyVaultUri, testSecretName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _client.DeleteSecretAsync(_keyVaultUri, Name);
        }
    }
}
