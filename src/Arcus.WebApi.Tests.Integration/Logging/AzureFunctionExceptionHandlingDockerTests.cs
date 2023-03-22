using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection(Constants.TestCollections.Docker)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Docker)]
    public class AzureFunctionExceptionHandlingDockerTests
    {
        private readonly TestConfig _config;
        private readonly ILogger _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionExceptionHandlingDockerTests" /> class.
        /// </summary>
        public AzureFunctionExceptionHandlingDockerTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
            _config = TestConfig.Create();
        }

        private string SabotageEndpoint => $"http://localhost:{_config.GetDockerAzureFunctionsIsolatedHttpPort()}/api/sabotage";

        [Fact]
        public async Task SendRequest_SabotageEndpoint_CatchesFailure()
        {
            // Act
            _logger.LogInformation("GET -> '{Uri}'", SabotageEndpoint);
            using (HttpResponseMessage response = await HttpClient.GetAsync(SabotageEndpoint))
            {
                // Assert
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                string contents = await response.Content.ReadAsStringAsync();
                Assert.Equal("Failed to process request due to a server failure", contents);
            }
        }
    }
}
