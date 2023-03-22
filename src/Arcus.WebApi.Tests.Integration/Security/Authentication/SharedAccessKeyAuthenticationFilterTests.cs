using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Testing.Logging;
using Arcus.Testing.Security.Providers.InMemory;
using Arcus.WebApi.Tests.Integration.Controllers;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Fixture;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// Ignore obsolete warnings.
#pragma warning disable CS0618

namespace Arcus.WebApi.Tests.Integration.Security.Authentication
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class SharedAccessKeyAuthenticationFilterTests
    {
        private const string HeaderName = "x-shared-access-key",
                             QueryParameterName = "sharedAccessKey",
                             SecretName = "custom-access-key-name";

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationFilterTests"/> class.
        /// </summary>
        public SharedAccessKeyAuthenticationFilterTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Theory]
        [InlineData(BypassOnMethodController.SharedAccessKeyRoute)]
        [InlineData(BypassSharedAccessKeyController.BypassOverAuthenticationRoute)]
        [InlineData(AllowAnonymousSharedAccessKeyController.Route)]
        public async Task SharedAccessKeyAuthorizedRoute_WithBypassAttributeOnMethod_SkipsAuthentication(string route)
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnHeader(HeaderName, SecretName)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(route);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyAuthorizedRoute_DoesntEmitSecurityEventsByDefault_RunsAuthentication()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                    services.AddSecretStore(stores => stores.AddInMemory(SecretName, $"secret-{Guid.NewGuid()}"))
                            .AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnHeader(HeaderName, SecretName)))
                .ConfigureHost(host => host.UseSerilog((context, config) =>
                    config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);

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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SharedAccessKeyAuthorizedRoute_EmitsSecurityEventsWhenRequested_RunsAuthentication(bool emitsSecurityEvents)
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores => stores.AddInMemory(SecretName, $"secret-{Guid.NewGuid()}"))
                            .AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnHeader(HeaderName, SecretName, authOptions =>
                            {
                                authOptions.EmitSecurityEvents = emitsSecurityEvents;
                            }));
                })
                .ConfigureHost(host => host.UseSerilog((context, config) =>
                    config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    IEnumerable<LogEvent> logEvents = spySink.DequeueLogEvents();
                    Assert.True(emitsSecurityEvents == logEvents.Any(logEvent =>
                    {
                        string message = logEvent.RenderMessage();
                        return message.Contains("EventType") && message.Contains("Security");
                    }));
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyOnHeader_WithInvalidFirstSecret_StillSucceedsWithValidSecondSecret()
        {
            // Arrange
            string secret1 = $"secret-{Guid.NewGuid()}", secret2 = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores =>
                    {
                        stores.AddProvider(
                            new InMemoryVersionedSecretProvider(new Secret(secret1), new Secret(secret2)),
                            opt => opt.AddVersionedSecret(SecretName, 2));
                    });
                    services.AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnHeader(HeaderName, SecretName));
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request =
                    HttpRequestBuilder.Get(HealthController.GetRoute)
                                      .WithHeader(HeaderName, secret2);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyOnHeader_WithInvalidFirstSecretButWithoutConfiguredVersions_DiscardsSecondSecret()
        {
            // Arrange
            string secret1 = $"secret-{Guid.NewGuid()}", secret2 = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores => stores.AddProvider(new InMemoryVersionedSecretProvider(new Secret(secret1), new Secret(secret2))));
                    services.AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnHeader(HeaderName, SecretName));
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request =
                    HttpRequestBuilder.Get(HealthController.GetRoute)
                                      .WithHeader(HeaderName, secret2);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyOnHeader_WithInvalidFirstSecretButWithoutConfiguredSecondSecret_Fails()
        {
            // Arrange
            string secret1 = $"secret-{Guid.NewGuid()}", secret2 = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores =>
                    {
                        stores.AddProvider(
                            new InMemoryVersionedSecretProvider(new Secret(secret1), new Secret(secret2)),
                            opt => opt.AddVersionedSecret(SecretName, 1));
                    });
                    services.AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnHeader(HeaderName, SecretName));
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request =
                    HttpRequestBuilder.Get(HealthController.GetRoute)
                                      .WithHeader(HeaderName, secret2);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyOnQuery_WithInvalidFirstSecret_StillSucceedsWithValidSecondSecret()
        {
            // Arrange
            string secret1 = $"secret-{Guid.NewGuid()}", secret2 = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores =>
                    {
                        stores.AddProvider(
                            new InMemoryVersionedSecretProvider(new Secret(secret1), new Secret(secret2)),
                            opt => opt.AddVersionedSecret(SecretName, 2));
                    });
                    services.AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnQuery(QueryParameterName, SecretName));
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request =
                    HttpRequestBuilder.Get(HealthController.GetRoute)
                                      .WithParameter(QueryParameterName, secret2);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyOnQuery_WithInvalidFirstSecretButWithoutConfiguredVersions_DiscardsSecondSecret()
        {
            // Arrange
            string secret1 = $"secret-{Guid.NewGuid()}", secret2 = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores => stores.AddProvider(new InMemoryVersionedSecretProvider(new Secret(secret1), new Secret(secret2))));
                    services.AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnQuery(QueryParameterName, SecretName));
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request =
                    HttpRequestBuilder.Get(HealthController.GetRoute)
                                      .WithParameter(QueryParameterName, secret2);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task SharedAccessKeyOnQuery_WithInvalidFirstSecretButWithoutConfiguredSecondSecret_Fails()
        {
            // Arrange
            string secret1 = $"secret-{Guid.NewGuid()}", secret2 = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores =>
                    {
                        stores.AddProvider(
                            new InMemoryVersionedSecretProvider(new Secret(secret1), new Secret(secret2)),
                            opt => opt.AddVersionedSecret(SecretName, 1));
                    });
                    services.AddControllers(opt => opt.AddSharedAccessKeyAuthenticationFilterOnQuery(QueryParameterName, SecretName));
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request =
                    HttpRequestBuilder.Get(HealthController.GetRoute)
                                      .WithParameter(QueryParameterName, secret2);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
    }
}
