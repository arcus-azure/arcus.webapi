using System;
using System.Threading.Tasks;
using Arcus.Security.Core.Caching;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.WebApi.Integration.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Integration.Jobs
{
    public class AutoInvalidateKeyVaultSecretJobTests : IAsyncLifetime
    {
        private readonly TestConfig _config;
        private readonly TestHost _host;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoInvalidateKeyVaultSecretJobTests"/> class.
        /// </summary>
        public AutoInvalidateKeyVaultSecretJobTests(ITestOutputHelper outputWriter)
        {
            _config = TestConfig.Create();
            _host = new TestHost(_config, outputWriter);
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task NewSecretVersion_TriggersKeyVaultJob_AutoInvalidatesSecret()
        {
            // Arrange
            var applicationId = _config.GetValue<string>("Arcus:ServicePrincipal:ApplicationId");
            var clientKey = _config.GetValue<string>("Arcus:ServicePrincipal:AccessKey");
            var keyVaultUri = _config.GetValue<string>("Arcus:KeyVault:Uri");
            var authentication = new ServicePrincipalAuthentication(applicationId, clientKey);
            var cachedSecretProvider = _host.Services.GetService<ICachedSecretProvider>();
            
            using (IKeyVaultClient client = await authentication.AuthenticateAsync()) 
            // Act
            await using (var tempSecret = await TempSecret.CreateNewAsync(client, keyVaultUri))
            {
                // Assert
                RetryAssertion(
                    // ReSharper disable once AccessToDisposedClosure - disposal happens after retry.
                    () => Mock.Get(cachedSecretProvider)
                              .Verify(p => p.InvalidateSecretAsync(It.Is<string>(n => n == tempSecret.Name)), Times.Once), 
                    timeout: TimeSpan.FromSeconds(30),
                    interval: TimeSpan.FromMilliseconds(500));
            }
        }

        private static void RetryAssertion(Action assertion, TimeSpan timeout, TimeSpan interval)
        {
            Policy.Timeout(timeout)
                  .Wrap(Policy.Handle<MockException>()
                              .WaitAndRetryForever(_ => interval))
                  .Execute(assertion);
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        async Task IAsyncLifetime.DisposeAsync()
        {
            var host = _host.Services.GetService<IHost>();
            await host.StopAsync();

            _host?.Dispose();
        }
    }
}
