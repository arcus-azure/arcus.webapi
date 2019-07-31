using Microsoft.Extensions.Configuration;
using Xunit;
using Arcus.WebApi.Security.Configuration;
using System.Collections.Generic;

namespace Arcus.WebApi.Unit.Security.Configuration
{
    public class SecretProviderConfigurationBuilderExtensionsTests
    {
        [Fact]
        public void AddKeyVault_Accesses_SecretProvider_For_Secret_Values_From_Configuration_Tokens()
        {
            // Arrange
            const string configurationKey = "ConnectionString";
            const string expected = "connection to somewhere";

            var stubProvider = new InMemorySecretProvider((configurationKey, expected));

            var configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new [] { new KeyValuePair<string, string>(configurationKey, "#{ConnectionString}#") })
                    .AddAzureKeyVault(stubProvider)
                    .Build();

            // Act
            IConfigurationSection section = configuration.GetSection(configurationKey);

            // Assert
            Assert.Equal(expected, section.Value);
        }
    }
}
