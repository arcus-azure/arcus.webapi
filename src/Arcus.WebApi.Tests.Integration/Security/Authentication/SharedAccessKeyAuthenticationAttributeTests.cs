using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Testing.Logging;
using Arcus.Testing.Security.Providers.InMemory;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Arcus.WebApi.Tests.Integration.Controllers;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class SharedAccessKeyAuthenticationAttributeTests
    {
        private const string HeaderName = "x-shared-access-key",
                             HeaderNameUpper = "X-SHARED-ACCESS-KEY",
                             SecretName = "custom-access-key-name",
                             ParameterName = "api-key",
                             ParameterNameUpper = "API-KEY";

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationAttributeTests"/> class.
        /// </summary>
        public SharedAccessKeyAuthenticationAttributeTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithKeyNotFoundException_WhenNoSecretProviderWasRegistered()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions();

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRoute)
                    .WithHeader(HeaderName, secretValue);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ISecretProvider), HeaderName)]
        [InlineData(typeof(ISecretProvider), HeaderNameUpper)]
        [InlineData(typeof(ICachedSecretProvider), HeaderName)]
        [InlineData(typeof(ICachedSecretProvider), HeaderNameUpper)]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithSecretProvider_ShouldNotFailWithUnauthorized(Type secretProviderType, string headerName)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteHeader)
                    .WithHeader(headerName, secretValue);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(HeaderName, "some header value")]
        [InlineData("some.header.name", "some header value")]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized(string headerName, string headerValue)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteHeader)
                    .WithHeader(headerName, headerValue);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenAnyHeaderValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteHeader)
                    .WithHeader(HeaderName, $"{secretValue};second header value other than {nameof(secretValue)}");
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ISecretProvider), ParameterName)]
        [InlineData(typeof(ISecretProvider), ParameterNameUpper)]
        [InlineData(typeof(ICachedSecretProvider), ParameterName)]
        [InlineData(typeof(ICachedSecretProvider), ParameterNameUpper)]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnQueryParameter_RegisteredWIthSecretProvider_ShouldNotFailWithUnauthorized(
            Type secretProviderType, string parameterName)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteQueryString)
                    .WithParameter(parameterName, secretValue);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(ParameterName, "some parameter value")]
        [InlineData("some parameter name", "some parameter value")]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnQueryParameter_ShouldFailWithUnauthorized(string parameterName, string parameterValue)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteQueryString)
                    .WithParameter(parameterName, parameterValue);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ISecretProvider))]
        [InlineData(typeof(ICachedSecretProvider))]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnBothHeaderAndQueryParameter_ShouldNotFailWithUnauthorized(Type secretProviderType)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRoute)
                    .WithHeader(HeaderName, secretValue)
                    .WithParameter(ParameterName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData("some header value", "some parameter value")]
        [InlineData("some header value", "secret")]
        [InlineData("secret", "some parameter value")]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnBothHeaderAndQueryParameter_ShouldFailWithUnauthorized(string headerValue, string parameterValue)
        {
            // Arrange
            string secretValue = "secret";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRoute)
                    .WithHeader(HeaderName, headerValue)
                    .WithParameter(ParameterName, parameterValue);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task SharedAccessKeyAuthorizedRoute_DoesntEmitSecurityEventsByDefault_RunsAuthentication()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, $"secret-{Guid.NewGuid()}")))
                .ConfigureHost(host => host.UseSerilog((context, config) => 
                    config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(SharedAccessKeyAuthenticationController.AuthorizedGetRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    IEnumerable<LogEvent> logEvents = spySink.DequeueLogEvents();
                    Assert.DoesNotContain(logEvents, logEvent =>
                    {
                        string message = logEvent.RenderMessage();
                        return message.Contains("EventType") && message.Contains("Security");
                    });
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyAuthorizedRoute_EmitsSecurityEventsWhenRequested_RunsAuthentication()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, $"secret-{Guid.NewGuid()}")))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteEmitSecurityEvents);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    IEnumerable<LogEvent> logEvents = spySink.DequeueLogEvents();
                    Assert.Contains(logEvents, logEvent =>
                    {
                        string message = logEvent.RenderMessage();
                        return message.Contains("EventType") && message.Contains("Security");
                    });
                }
            }
        }
    }
}
