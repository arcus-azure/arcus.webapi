using Newtonsoft.Json;

namespace Arcus.WebApi.Jobs.KeyVault 
{
    /// <summary>
    /// Azure Key Vault secret event, when a new version is created for the secret.
    /// </summary>
    public class SecretNewVersionCreated
    {
        [JsonProperty("id")]
        public string Id { get; set; }
 
        [JsonProperty("vaultName")]
        public string VaultName { get; set; }
        
        [JsonProperty("objectType")]
        public string ObjectType { get; set; }
        
        [JsonProperty("objectName")]
        public string ObjectName { get; set; }
    }
}