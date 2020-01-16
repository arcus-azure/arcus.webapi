using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.WebApi.Jobs.KeyVault;
using GuardNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Arcus.WebApi.Integration.Hosting
{
    /// <summary>
    /// Represents a <see cref="Host"/> using to simulate a real-life application when integration testing the web API components.
    /// </summary>
    public class TestHost : WebApplicationFactory<TestStartup>
    {
        private readonly TestConfig _config;
        private readonly ILogger _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHost"/> class.
        /// </summary>
        public TestHost(TestConfig config, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            _config = config;
            _outputWriter = new XunitTestLogger(outputWriter);
        }

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> used to set up <see cref="T:Microsoft.AspNetCore.TestHost.TestServer" />.
        /// </summary>
        /// <remarks>
        /// The default implementation of this method looks for a <c>public static IHostBuilder CreateHostBuilder(string[] args)</c>
        /// method defined on the entry point of the assembly of <see name="TestStartup" /> and invokes it passing an empty string
        /// array as arguments.
        /// </remarks>
        /// <returns>A <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</returns>
        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                       .ConfigureWebHost(webHost => webHost.UseStartup<TestStartup>())
                       .ConfigureAppConfiguration(config => config.AddInMemoryCollection(_config.AsEnumerable()))
                       .ConfigureServices(services =>
                       {
                           const string secretKey = "Arcus:ServiceBus:ConnectionStringWithTopic";

                           var cachedSecretProvider = new Mock<ICachedSecretProvider>();
                           cachedSecretProvider
                               .Setup(p => p.GetRawSecretAsync(secretKey))
                               .ReturnsAsync(() => _config[secretKey]);

                           cachedSecretProvider
                               .Setup(p => p.InvalidateSecretAsync(It.IsAny<string>()))
                               .Returns(Task.CompletedTask);

                           services.AddSingleton<ISecretProvider>(cachedSecretProvider.Object);
                           services.AddSingleton<ICachedSecretProvider>(cachedSecretProvider.Object);
                           services.AddSingleton(_outputWriter);
                           services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
                               subscriptionNamePrefix: $"Test-{Guid.NewGuid():N}",
                               serviceBusTopicConnectionStringSecretKey: secretKey);
                       });
        }
    }
}
