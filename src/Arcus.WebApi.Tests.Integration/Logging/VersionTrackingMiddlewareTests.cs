using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Controllers;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection("Integration")]
    public class VersionTrackingMiddlewareTests
    {
        private const string DefaultHeaderName = "X-Version";

        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionTrackingMiddlewareTests" /> class.
        /// </summary>
        public VersionTrackingMiddlewareTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }
        
        [Fact]
        public async Task SendRequest_WithVersionTracking_AddsApplicationVersionToResponse()
        {
            // Arrange
            string expected = $"version-{Guid.NewGuid()}";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddAppVersion(provider => new StubAppVersion(expected)))
                .Configure(app => app.UseVersionTracking());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.True(response.Headers.TryGetValues(DefaultHeaderName, out IEnumerable<string> values));
                    Assert.Equal(expected, Assert.Single(values));
                }
            }
        }

        [Fact]
        public async Task SendRequest_WithVersionTrackingOnCustomHeaderName_AddsApplicationVersionToResponse()
        {
            // Arrange
            string headerName = $"header-name-{Guid.NewGuid()}";
            string expected = $"version-{Guid.NewGuid()}";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddAppVersion(provider => new StubAppVersion(expected)))
                .Configure(app => app.UseVersionTracking(opt => opt.HeaderName = headerName));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.False(response.Headers.Contains(DefaultHeaderName));
                    Assert.True(response.Headers.TryGetValues(headerName, out IEnumerable<string> values));
                    Assert.Equal(expected, Assert.Single(values));
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task SendRequest_WithVersionTrackingForBlankVersion_DoesntAddApplicationVersionToResponse(string version)
        {
            // Arrange
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddAppVersion(provider => new StubAppVersion(version)))
                .Configure(app => app.UseVersionTracking());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.False(response.Headers.Contains(DefaultHeaderName));
                }
            }
        }

        [Fact]
        public async Task SetupApi_WithoutApplicationVersion_Throws()
        {
            // Arrange
            var options = new ServerOptions()
                .Configure(app => app.UseVersionTracking());

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => TestApiServer.StartNewAsync(options, _logger));
        }
    }
}
