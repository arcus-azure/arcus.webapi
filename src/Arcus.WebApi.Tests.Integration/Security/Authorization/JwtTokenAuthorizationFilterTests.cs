using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Security.Authorization;
using Arcus.WebApi.Security.Authorization.Jwt;
using Arcus.WebApi.Tests.Integration.Controllers;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers;
using Arcus.WebApi.Tests.Integration.Security.Authorization.Controllers;
using Arcus.WebApi.Tests.Integration.Security.Authorization.Fixture;
using Bogus;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// ReSharper disable AccessToDisposedClosure

namespace Arcus.WebApi.Tests.Integration.Security.Authorization
{
    [Collection("Integration")]
    public class JwtTokenAuthorizationFilterTests
    {
        private readonly ILogger _logger;
        private readonly Faker _bogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationFilterTests"/> class.
        /// </summary>
        public JwtTokenAuthorizationFilterTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            string issuer = _bogusGenerator.Internet.Url();
            string authority = _bogusGenerator.Internet.Url();
            string privateKey = GenerateRandomPrivateKey();

            await using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_logger))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        TokenValidationParameters tokenValidationParameters = testOpenIdServer.GenerateTokenValidationParametersWithValidAudience(issuer, authority, privateKey);
                        var reader = new JwtTokenReader(tokenValidationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                        services.AddMvc(opt => opt.Filters.AddJwtTokenAuthorization(jwt => jwt.JwtTokenReader = reader));
                    });
                
                await using (var testApiServer = await TestApiServer.StartNewAsync(options, _logger))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, daysValid: 7);
                    var request = HttpRequestBuilder
                        .Get(HealthController.GetRoute)
                        .WithHeader(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                    // Act
                    using (HttpResponseMessage response = await testApiServer.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithNullReaderAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddMvc(opt =>
                    {
                        opt.Filters.AddJwtTokenAuthorization(jwt => jwt.AddJwtTokenReader(serviceProvider => null));
                    });
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithLazyAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            string issuer = _bogusGenerator.Internet.Url();
            string authority = _bogusGenerator.Internet.Url();
            string privateKey = GenerateRandomPrivateKey();

            await using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_logger))
            {
                TokenValidationParameters validationParameters = testOpenIdServer.GenerateTokenValidationParametersWithValidAudience(issuer, authority, privateKey);
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddMvc(opt =>
                        {
                            opt.Filters.AddJwtTokenAuthorization(jwt => jwt.AddJwtTokenReader(serviceProvider => reader));
                        });
                    });
                
                await using (var testApiServer = await TestApiServer.StartNewAsync(options, _logger))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, daysValid: 7);
                    var request = HttpRequestBuilder
                        .Get(HealthController.GetRoute)
                        .WithHeader(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                    // Act
                    using (HttpResponseMessage response = await testApiServer.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithInjectedAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddMvc(opt =>
                    {
                        opt.Filters.AddJwtTokenAuthorization(jwt => jwt.AddJwtTokenReader<IgnoredJwtTokenReader>());
                    });
                });
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}.{_bogusGenerator.Random.AlphaNumeric(40)}";
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithHeader(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetHealthWithIncorrectBearerToken_WithAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            string issuer = _bogusGenerator.Internet.Url();
            string authority = _bogusGenerator.Internet.Url();
            string privateKey = GenerateRandomPrivateKey();

            await using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_logger))
            {
                TokenValidationParameters validationParameters = testOpenIdServer.GenerateTokenValidationParametersWithValidAudience(issuer, authority, privateKey);
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);

                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddMvc(opt =>
                        {
                            opt.Filters.AddJwtTokenAuthorization(jwt => jwt.JwtTokenReader = reader);
                        });
                    });

                await using (var testApiServer = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}.{_bogusGenerator.Random.AlphaNumeric(40)}";
                    var request = HttpRequestBuilder
                        .Get(HealthController.GetRoute)
                        .WithHeader(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                    // Act
                    using (HttpResponseMessage response = await testApiServer.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task GetHealthWithIncorrectBase64BearerToken_WithAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddMvc(opt => opt.Filters.AddJwtTokenAuthorization()));
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                string accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}";
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithHeader(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithIncorrectAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            string issuer = _bogusGenerator.Internet.Url();
            string authority = _bogusGenerator.Internet.Url();
            string privateKey = GenerateRandomPrivateKey();

            await using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_logger))
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };

                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddMvc(opt => opt.Filters.AddJwtTokenAuthorization(jwt => jwt.JwtTokenReader = reader));
                    });

                await using (var testApiServer = await TestApiServer.StartNewAsync(options, _logger))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, daysValid: 7);
                    var request = HttpRequestBuilder
                        .Get(HealthController.GetRoute)
                        .WithHeader(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                    // Act
                    using (HttpResponseMessage response = await testApiServer.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task GetHealthWithoutBearerToken_WithIncorrectAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            await using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_logger))
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);

                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddMvc(opt =>
                        {
                            opt.Filters.AddJwtTokenAuthorization(jwt => jwt.JwtTokenReader = reader);
                        });
                    });

                await using (var testApiServer = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(HealthController.GetRoute);

                    // Act
                    using (HttpResponseMessage response = await testApiServer.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Theory]
        [InlineData(BypassOnMethodController.JwtRoute)]
        [InlineData(BypassJwtTokenAuthorizationController.BypassOverAuthorizationRoute)]
        [InlineData(BypassOnMethodController.AllowAnonymousRoute)]
        public async Task JwtAuthorizedRoute_WithBypassAttribute_SkipsAuthorization(string route)
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddMvc(opt => opt.Filters.AddJwtTokenAuthorization()));

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
        public async Task JwtAuthorizedRoute_DoesntEmitSecurityEventsByDefault_RunsAuthorization()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddMvc(opt => opt.Filters.AddJwtTokenAuthorization()))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                string accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}";
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithHeader(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

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
        public async Task JwtAuthorizedRoute_EmitSecurityEventsWhenRequested_RunsAuthorization(bool emitSecurityEvents)
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddMvc(opt => opt.Filters.AddJwtTokenAuthorization(jwt => jwt.EmitSecurityEvents = emitSecurityEvents));
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                string accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}";
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithHeader(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    IEnumerable<LogEvent> logEvents = spySink.DequeueLogEvents();
                    Assert.True(emitSecurityEvents == logEvents.Any(logEvent =>
                    {
                        string message = logEvent.RenderMessage();
                        return message.Contains("EventType") && message.Contains("Security");
                    }));
                }
            }
        }

        private static string GenerateRandomPrivateKey()
        {
            using (RSA rsa = new RSACryptoServiceProvider(dwKeySize: 2048))
            {
                string privateKey = rsa.ToCustomXmlString(includePrivateParameters: true);
                return privateKey;
            }
        }
    }
}