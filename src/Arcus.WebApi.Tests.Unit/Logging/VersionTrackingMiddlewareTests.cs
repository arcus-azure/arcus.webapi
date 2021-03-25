using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
using Arcus.WebApi.Tests.Unit.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class VersionTrackingMiddlewareTests : IDisposable
    {
        private const string DefaultHeaderName = "X-Version";

        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task SendRequest_WithVersionTracking_AddsApplicationVersionToResponse()
        {
            // Arrange
            string expected = $"version-{Guid.NewGuid()}";
            _testServer.AddServicesConfig(services => services.AddSingleton<IAppVersion>(provider => new StubAppVersion(expected)));
            _testServer.AddConfigure(app => app.UseVersionTracking());

            using (HttpClient client = _testServer.CreateClient())
            {
                // Act
                using (HttpResponseMessage response = await client.GetAsync(EchoController.Route))
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
            _testServer.AddServicesConfig(services => services.AddSingleton<IAppVersion>(provider => new StubAppVersion(expected)));
            _testServer.AddConfigure(app => app.UseVersionTracking(options => options.HeaderName = headerName));

            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(EchoController.Route))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.False(response.Headers.Contains(DefaultHeaderName));
                Assert.True(response.Headers.TryGetValues(headerName, out IEnumerable<string> values));
                Assert.Equal(expected, Assert.Single(values));
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task SendRequest_WithVersionTrackingForBlankVersion_DoesntAddApplicationVersionToResponse(string version)
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.AddSingleton<IAppVersion>(provider => new StubAppVersion(version)));
            _testServer.AddConfigure(app => app.UseVersionTracking());

            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(EchoController.Route))
            {
                // Assert
                Assert.False(response.Headers.Contains(DefaultHeaderName));
            }
        }

        [Fact]
        public void SetupApi_WithoutApplicationVersion_Throws()
        {
            // Arrange
            _testServer.AddConfigure(app => app.UseVersionTracking());

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => _testServer.CreateClient());
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _testServer.Dispose();
        }
    }
}
