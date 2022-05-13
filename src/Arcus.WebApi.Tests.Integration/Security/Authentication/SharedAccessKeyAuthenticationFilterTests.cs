using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Arcus.WebApi.Tests.Integration.Controllers;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication
{
    [Collection("Integration")]
    public class SharedAccessKeyAuthenticationFilterTests
    {
        private const string HeaderName = "x-shared-access-key",
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
                .ConfigureServices(services => services.AddMvc(opt => opt.Filters.AddSharedAccessKeyAuthenticationOnHeader(HeaderName, SecretName)));

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
                            .AddMvc(opt => opt.Filters.AddSharedAccessKeyAuthenticationOnHeader(HeaderName, SecretName)))
                .ConfigureHost(host => host.UseSerilog((context, config) => 
                    config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(DefaultController.GetRoute);
                
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
                            .AddMvc(opt => opt.Filters.AddSharedAccessKeyAuthenticationOnHeader(HeaderName, SecretName, authOptions =>
                            {
                                authOptions.EmitSecurityEvents = emitsSecurityEvents;
                            }));
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => 
                    config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(DefaultController.GetRoute);
                
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
    }
}
