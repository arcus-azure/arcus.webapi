using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Security.Authentication.Certificates;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Fixture;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using static Arcus.WebApi.Security.Authentication.Certificates.X509ValidationLocation;
using ILogger = Microsoft.Extensions.Logging.ILogger;
// ReSharper disable AccessToDisposedClosure

namespace Arcus.WebApi.Tests.Integration.Security.Authentication
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class CertificateAuthenticationAttributeTests
    {
        public const string SubjectKey = "subject", 
                            IssuerKey = "isser", 
                            ThumbprintKey = "thumbprint";

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationAttributeTests" /> class.
        /// </summary>
        public CertificateAuthenticationAttributeTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }
        
        [Theory]
        [InlineData("known-subject")]
        [InlineData("unknown-subject")]
        public async Task AuthorizedRoute_WithCertificateAuthenticationOnAppServiceHeader_ShouldSucceeds_WhenClientCertificateSubjectNameMatches(string actualSubject)
        {
            // Arrange
            const string expectedSubject = "known-subject";
            using (var cert = SelfSignedCertificate.CreateWithSubject(expectedSubject))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(SubjectKey, $"CN={actualSubject}"))
                                .AddCertificateAuthenticationValidation(auth => auth.WithSubject(SecretProvider, SubjectKey));
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    string clientCertificate = Convert.ToBase64String(cert.RawData);
                    var request = HttpRequestBuilder
                        .Get(CertificateAuthenticationOnMethodController.AuthorizedGetRoute)
                        .WithHeader("X-ARR-ClientCert", clientCertificate);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Arrange
                        bool equalSubject = expectedSubject == actualSubject;
                        bool isUnauthorized = response.StatusCode == HttpStatusCode.Unauthorized;
                        Assert.True(equalSubject != isUnauthorized, "Client certificate with the same subject name should result in an OK HTTP status code");
                    } 
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(SubjectKey, "CN=subject"))
                                .AddClientCertificate(clientCertificate)
                                .AddCertificateAuthenticationValidation(auth => auth.WithSubject(SecretProvider, SubjectKey));
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(CertificateAuthenticationOnMethodController.AuthorizedGetRoute);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Arrange
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Theory]
        [InlineData("subject", "issuer", "", false)]
        [InlineData("subject", "issuer", "unrecognized-thumbprint", true)]
        [InlineData("subject", "unrecognized-issuer", "", true)]
        [InlineData("subject", "unrecognized-issuer", "unrecognized-thumbprint", true)]
        [InlineData("unrecognized-subject", "issuer", "", true)]
        [InlineData("unrecognized-subject", "issuer", "unrecognized-thumbprint", true)]
        [InlineData("unrecognized-subject", "unrecognized-issuer", "", true)]
        [InlineData("unrecognized-subject", "unrecognized-issuer", "unrecognized-thumbprint", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaSecretProvider_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectName,
            string issuerName,
            string thumbprintNoise,
            bool expected)
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerName, subjectName))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddCertificateAuthenticationValidation(auth =>
                                {
                                    auth.WithSubject(SecretProvider, SubjectKey)
                                        .WithIssuer(SecretProvider, IssuerKey)
                                        .WithThumbprint(SecretProvider, ThumbprintKey);
                                })
                                .AddClientCertificate(clientCertificate)
                                .AddSecretStore(stores => stores.AddInMemory(new Dictionary<string, string>
                                {
                                    [SubjectKey] = "CN=subject",
                                    [IssuerKey] = "CN=issuer",
                                    [ThumbprintKey] = clientCertificate.Thumbprint + thumbprintNoise
                                }));
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(CertificateAuthenticationOnMethodController.AuthorizedGetRoute);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.True(
                            (HttpStatusCode.Unauthorized == response.StatusCode) == expected,
                            $"Response HTTP status code {(expected ? "should" : "shouldn't")} be 'Unauthorized' but was '{response.StatusCode}'");
                    }
                }
            }
        }

        [Theory]
        [InlineData("subject", "issuer", "", false)]
        [InlineData("subject", "issuer", "unrecognized-thumbprint", true)]
        [InlineData("subject", "unrecognized-issuer", "", true)]
        [InlineData("subject", "unrecognized-issuer", "unrecognized-thumbprint", true)]
        [InlineData("unrecognized-subject", "issuer", "", true)]
        [InlineData("unrecognized-subject", "issuer", "unrecognized-thumbprint", true)]
        [InlineData("unrecognized-subject", "unrecognized-issuer", "", true)]
        [InlineData("unrecognized-subject", "unrecognized-issuer", "unrecognized-thumbprint", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaConfiguration_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectName,
            string issuerName,
            string thumbprintNoise,
            bool expected)
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerName, subjectName))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(SubjectKey, "CN=subject"),
                        new KeyValuePair<string, string>(IssuerKey, "CN=issuer"),
                        new KeyValuePair<string, string>(ThumbprintKey, clientCertificate.Thumbprint + thumbprintNoise)
                    }))
                    .ConfigureServices(services =>
                    {
                        services.AddCertificateAuthenticationValidation(auth =>
                                {
                                    auth.WithSubject(Configuration, SubjectKey)
                                        .WithIssuer(Configuration, IssuerKey)
                                        .WithThumbprint(Configuration, ThumbprintKey);
                                })
                                .AddClientCertificate(clientCertificate);
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(CertificateAuthenticationOnMethodController.AuthorizedGetRoute);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.True(
                            (HttpStatusCode.Unauthorized == response.StatusCode) == expected,
                            $"Response HTTP status code {(expected ? "should" : "shouldn't")} be 'Unauthorized' but was '{response.StatusCode}'");
                    }
                }
            }
        }

        [Theory]
        [InlineData("subject", "issuer", "", false)]
        [InlineData("subject", "issuer", "unrecognized-thumbprint", true)]
        [InlineData("subject", "unrecognized-issuer", "", true)]
        [InlineData("subject", "unrecognized-issuer", "unrecognized-thumbprint", true)]
        [InlineData("unrecognized-subject", "issuer", "", true)]
        [InlineData("unrecognized-subject", "issuer", "unrecognized-thumbprint", true)]
        [InlineData("unrecognized-subject", "unrecognized-issuer", "", true)]
        [InlineData("unrecognized-subject", "unrecognized-issuer", "unrecognized-thumbprint", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaConfigurationSecretProviderAndCustom_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectName,
            string issuerName,
            string thumbprintNoise,
            bool expected)
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerName, subjectName))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(SubjectKey, "CN=subject")
                    }))
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(IssuerKey, "CN=issuer"))
                                .AddClientCertificate(clientCertificate)
                                .AddCertificateAuthenticationValidation(auth =>
                                {
                                    auth.WithSubject(Configuration, SubjectKey)
                                        .WithIssuer(SecretProvider, IssuerKey)
                                        .WithThumbprint(new StubX509ValidationLocation(clientCertificate.Thumbprint + thumbprintNoise), ThumbprintKey);
                                });
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(CertificateAuthenticationOnMethodController.AuthorizedGetRoute);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.True(
                            (HttpStatusCode.Unauthorized == response.StatusCode) == expected,
                            $"Response HTTP status code {(expected ? "should" : "shouldn't")} be 'Unauthorized' but was '{response.StatusCode}'");
                    }
                }
            }
        }
        
        [Fact]
        public async Task CertificateAuthorizedRoute_DoesntEmitSecurityEventsByDefault_RunsAuthentication()
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"))
            {
                var spySink = new InMemorySink();
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(SubjectKey, "CN=subject"))
                                .AddClientCertificate(clientCertificate)
                                .AddCertificateAuthenticationValidation(auth => auth.WithSubject(SecretProvider, SubjectKey));
                    })
                    .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(CertificateAuthenticationOnMethodController.AuthorizedGetRoute);

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
        }
        
        [Fact]
        public async Task CertificateAuthorizedRoute_EmitsSecurityEventsWhenRequested_RunsAuthentication()
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"))
            {
                var spySink = new InMemorySink();
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(SubjectKey, "CN=subject"))
                                .AddClientCertificate(clientCertificate)
                                .AddCertificateAuthenticationValidation(auth => auth.WithSubject(SecretProvider, SubjectKey));
                    })
                    .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(CertificateAuthenticationOnMethodController.AuthorizedGetRouteEmitSecurityEvents);

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
}
