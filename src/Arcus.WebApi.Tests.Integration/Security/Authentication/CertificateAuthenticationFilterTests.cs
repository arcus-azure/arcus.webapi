using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Security.Authentication.Certificates;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Arcus.WebApi.Tests.Integration.Controllers;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Fixture;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using static Arcus.WebApi.Security.Authentication.Certificates.X509ValidationLocation;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// Ignore obsolete warnings.
#pragma warning disable CS0618

namespace Arcus.WebApi.Tests.Integration.Security.Authentication
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class CertificateAuthenticationFilterTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationFilterTests"/> class.
        /// </summary>
        public CertificateAuthenticationFilterTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthenticationOnFilters_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            string subjectKey = "subject", subjectValue = $"subject-{Guid.NewGuid()}";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        var certificateValidator = 
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(SecretProvider, subjectKey)
                                    .Build());

                        services.AddSecretStore(stores => stores.AddInMemory(subjectKey, subjectValue))
                                .AddSingleton(certificateValidator)
                                .AddClientCertificate(clientCertificate)
                                .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            string subjectKey = "subject", subjectValue = $"subject-{Guid.NewGuid()}";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        var certificateValidator = 
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(SecretProvider, subjectKey)
                                    .Build());

                        services.AddSecretStore(stores => stores.AddInMemory(subjectKey, subjectValue))
                                .AddSingleton(certificateValidator)
                                .AddClientCertificate(clientCertificate)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthenticationWithDirectValidator_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            string subjectKey = "subject", subjectValue = $"subject-{Guid.NewGuid()}";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(subjectKey, subjectValue))
                                .AddClientCertificate(clientCertificate)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter(auth => auth.WithSubject(SecretProvider, subjectKey)));
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("thumbprint-noise", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationOnFilters_ShouldFailWithUnauthorized_WhenClientCertificateThumbprintDoesntMatch(
            string thumbprintNoise,
            bool expected)
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.Create())
            {
                const string thumbprintKey = "thumbprint";
                
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithThumbprint(SecretProvider, thumbprintKey)
                                    .Build());
                        
                        services.AddSecretStore(stores => stores.AddInMemory(thumbprintKey, clientCertificate.Thumbprint + thumbprintNoise))
                                .AddSingleton(certificateValidator)
                                .AddClientCertificate(clientCertificate)
                                .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("", false)]
        [InlineData("thumbprint-noise", true)]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateThumbprintDoesntMatch(
            string thumbprintNoise,
            bool expected)
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.Create())
            {
                const string thumbprintKey = "thumbprint";
                
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithThumbprint(SecretProvider, thumbprintKey)
                                    .Build());
                        
                        services.AddSecretStore(stores => stores.AddInMemory(thumbprintKey, clientCertificate.Thumbprint + thumbprintNoise))
                                .AddSingleton(certificateValidator)
                                .AddClientCertificate(clientCertificate)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("", false)]
        [InlineData("thumbprint-noise", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationWithDirectValidator_ShouldFailWithUnauthorized_WhenClientCertificateThumbprintDoesntMatch(
            string thumbprintNoise,
            bool expected)
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.Create())
            {
                const string thumbprintKey = "thumbprint";
                
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(thumbprintKey, clientCertificate.Thumbprint + thumbprintNoise))
                                .AddClientCertificate(clientCertificate)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter(auth => auth.WithThumbprint(SecretProvider, thumbprintKey)));
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaSecretProviderOnFilters_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(SecretProvider, subjectKey)
                                    .WithIssuer(SecretProvider, issuerKey)
                                    .Build());

                        services.AddClientCertificate(clientCertificate)
                                .AddSingleton(certificateValidator)
                                .AddSecretStore(stores => stores.AddInMemory(new Dictionary<string, string>
                                {
                                    [subjectKey] = "CN=known-subject",
                                    [issuerKey] = "CN=known-issuername"
                                }))
                                .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaSecretProvider_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(SecretProvider, subjectKey)
                                    .WithIssuer(SecretProvider, issuerKey)
                                    .Build());

                        services.AddClientCertificate(clientCertificate)
                                .AddSingleton(certificateValidator)
                                .AddSecretStore(stores => stores.AddInMemory(new Dictionary<string, string>
                                {
                                    [subjectKey] = "CN=known-subject",
                                    [issuerKey] = "CN=known-issuername"
                                }))
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationWithDirectValidatorViaSecretProvider_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddClientCertificate(clientCertificate)
                                .AddSecretStore(stores => stores.AddInMemory(new Dictionary<string, string>
                                {
                                    [subjectKey] = "CN=known-subject",
                                    [issuerKey] = "CN=known-issuername"
                                }))
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter(auth =>
                                {
                                    auth.WithSubject(SecretProvider, subjectKey)
                                        .WithIssuer(SecretProvider, issuerKey);
                                }));
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaConfigurationOnFilters_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject"),
                        new KeyValuePair<string, string>(issuerKey, "CN=known-issuername")
                    }))
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(Configuration, subjectKey)
                                    .WithIssuer(Configuration, issuerKey)
                                    .Build());

                        services.AddSingleton(certificateValidator)
                                .AddClientCertificate(clientCertificate)
                                .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaConfiguration_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject"),
                        new KeyValuePair<string, string>(issuerKey, "CN=known-issuername")
                    }))
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(Configuration, subjectKey)
                                    .WithIssuer(Configuration, issuerKey)
                                    .Build());

                        services.AddSingleton(certificateValidator)
                                .AddClientCertificate(clientCertificate)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationWithDirectValidatorViaConfiguration_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject"),
                        new KeyValuePair<string, string>(issuerKey, "CN=known-issuername")
                    }))
                    .ConfigureServices(services =>
                    {
                        services.AddClientCertificate(clientCertificate)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter(auth =>
                                {
                                    auth.WithSubject(Configuration, subjectKey)
                                        .WithIssuer(Configuration, issuerKey);
                                }));
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaConfigurationAndSecretProviderOnFilters_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject")
                    }))
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(Configuration, subjectKey)
                                    .WithIssuer(SecretProvider, issuerKey)
                                    .Build());

                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=known-issuername"))
                                .AddClientCertificate(clientCertificate)
                                .AddSingleton(certificateValidator)
                                .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaConfigurationAndSecretProvider_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject")
                    }))
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(Configuration, subjectKey)
                                    .WithIssuer(SecretProvider, issuerKey)
                                    .Build());

                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=known-issuername"))
                                .AddClientCertificate(clientCertificate)
                                .AddSingleton(certificateValidator)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationWithDirectValidtorViaConfigurationAndSecretProvider_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject")
                    }))
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=known-issuername"))
                                .AddClientCertificate(clientCertificate)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter(auth =>
                                {
                                    auth.WithSubject(Configuration, subjectKey)
                                        .WithIssuer(SecretProvider, issuerKey);
                                }));
                    });

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);

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
        public async Task AuthorizedRoute_WithCertificateAuthenticationOnFilters_ShouldFailOnInvalidBase64Format()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    var certificateValidator =
                        new CertificateAuthenticationValidator(
                            new CertificateAuthenticationConfigBuilder()
                                .WithSubject(Configuration, "ignored-subject")
                                .Build());

                    services.AddSingleton(certificateValidator)
                            .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(NoneAuthenticationController.GetRoute)
                    .WithHeader("X-ARR-ClientCert", "something not even close to an client certificate export");

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailOnInvalidBase64Format()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    var certificateValidator =
                        new CertificateAuthenticationValidator(
                            new CertificateAuthenticationConfigBuilder()
                                .WithSubject(Configuration, "ignored-subject")
                                .Build());

                    services.AddSingleton(certificateValidator)
                            .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(NoneAuthenticationController.GetRoute)
                    .WithHeader("X-ARR-ClientCert", "something not even close to an client certificate export");

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthenticationWithDirectValidator_ShouldFailOnInvalidBase64Format()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddControllers(opt =>
                    {
                        opt.AddCertificateAuthenticationFilter(auth => auth.WithSubject(Configuration, "ignored-subject"));
                    });
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(NoneAuthenticationController.GetRoute)
                    .WithHeader("X-ARR-ClientCert", "something not even close to an client certificate export");

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthenticationInHeaderOnFilters_ShouldSucceed()
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName("known-issuername", "known-subject"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject")
                    }))
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(Configuration, subjectKey)
                                    .WithIssuer(SecretProvider, issuerKey)
                                    .Build());
                        
                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=known-issuername"))
                                .AddSingleton(certificateValidator)
                                .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    string base64String = Convert.ToBase64String(clientCertificate.Export(X509ContentType.Pkcs12), Base64FormattingOptions.None);
                    var request = HttpRequestBuilder
                        .Get(NoneAuthenticationController.GetRoute)
                        .WithHeader("X-ARR-ClientCert", base64String);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthenticationInHeader_ShouldSucceed()
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName("known-issuername", "known-subject"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject")
                    }))
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithSubject(Configuration, subjectKey)
                                    .WithIssuer(SecretProvider, issuerKey)
                                    .Build());
                        
                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=known-issuername"))
                                .AddSingleton(certificateValidator)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    string base64String = Convert.ToBase64String(clientCertificate.Export(X509ContentType.Pkcs12), Base64FormattingOptions.None);
                    var request = HttpRequestBuilder
                        .Get(NoneAuthenticationController.GetRoute)
                        .WithHeader("X-ARR-ClientCert", base64String);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthenticationWithDirectValidatorInHeader_ShouldSucceed()
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName("known-issuername", "known-subject"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new []
                    {
                        new KeyValuePair<string, string>(subjectKey, "CN=known-subject")
                    }))
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=known-issuername"))
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter(auth =>
                                {
                                    auth.WithSubject(Configuration, subjectKey)
                                        .WithIssuer(SecretProvider, issuerKey);
                                }));
                    });
                
                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    string base64String = Convert.ToBase64String(clientCertificate.Export(X509ContentType.Pkcs12), Base64FormattingOptions.None);
                    var request = HttpRequestBuilder
                        .Get(NoneAuthenticationController.GetRoute)
                        .WithHeader("X-ARR-ClientCert", base64String);

                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }
        
        [Theory]
        [InlineData(BypassOnMethodController.CertificateRoute)]
        [InlineData(BypassCertificateController.BypassOverAuthenticationRoute)]
        [InlineData(AllowAnonymousCertificateController.Route)]
        public async Task CertificateAuthorizedRoute_WithBypassAttributeOnFilters_SkipsAuthentication(string route)
        {
            // Arrange
            const string issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName("issuer", "subject"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithIssuer(SecretProvider, issuerKey)
                                    .Build());

                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                                .AddClientCertificate(clientCertificate)
                                .AddSingleton(certificateValidator)
                                .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                    });

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
        }

        [Theory]
        [InlineData(BypassOnMethodController.CertificateRoute)]
        [InlineData(BypassCertificateController.BypassOverAuthenticationRoute)]
        [InlineData(AllowAnonymousCertificateController.Route)]
        public async Task CertificateAuthorizedRoute_WithBypassAttribute_SkipsAuthentication(string route)
        {
            // Arrange
            const string issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName("issuer", "subject"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        var certificateValidator =
                            new CertificateAuthenticationValidator(
                                new CertificateAuthenticationConfigBuilder()
                                    .WithIssuer(SecretProvider, issuerKey)
                                    .Build());

                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                                .AddClientCertificate(clientCertificate)
                                .AddSingleton(certificateValidator)
                                .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                    });

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
        }

        [Theory]
        [InlineData(BypassCertificateController.BypassOverAuthenticationRoute)]
        [InlineData(AllowAnonymousCertificateController.Route)]
        public async Task CertificateWithDirectValidatorAuthorizedRoute_WithBypassAttribute_SkipsAuthentication(string route)
        {
            // Arrange
            const string issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName("issuer", "subject"))
            {
                var options = new TestApiServerOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                                .AddClientCertificate(clientCertificate)
                                .AddCertificateAuthenticationValidation(auth => auth.WithIssuer(SecretProvider, issuerKey));
                    })
                    .Configure(app => app.UseExceptionHandling());

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
        }

        [Fact]
        public async Task CertificateWithDirectValidatorAuthorizedOnFilterByBypassedOnRoute_WithBypassAttribute_SkipsAuthentication()
        {
            // Arrange
            const string issuerKey = "issuer";
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName("issuer", "subject"))
            {
                var options = new TestApiServerOptions()
                              .ConfigureServices(services =>
                              {
                                  services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                                          .AddClientCertificate(clientCertificate)
                                          .AddControllers(opt => opt.AddCertificateAuthenticationFilter(auth => auth.WithIssuer(SecretProvider, issuerKey)));
                              })
                              .Configure(app => app.UseExceptionHandling());

                await using (var server = await TestApiServer.StartNewAsync(options, _logger))
                {
                    var request = HttpRequestBuilder.Get(BypassOnMethodController.CertificateRoute);
                    
                    // Act
                    using (HttpResponseMessage response = await server.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }
        }
        
        [Fact]
        public async Task CertificateAuthorizedRoute_DoesntEmitSecurityEventsByDefaultOnFilters_RunsAuthentication()
        {
            // Arrange
            const string issuerKey = "issuer";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    var certificateValidator =
                        new CertificateAuthenticationValidator(
                            new CertificateAuthenticationConfigBuilder()
                                .WithIssuer(SecretProvider, issuerKey)
                                .Build());

                    services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                            .AddSingleton(certificateValidator)
                            .AddMvc(opt => opt.Filters.AddCertificateAuthentication());
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);
                
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
        public async Task CertificateAuthorizedRoute_DoesntEmitSecurityEventsByDefault_RunsAuthentication()
        {
            // Arrange
            const string issuerKey = "issuer";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    var certificateValidator =
                        new CertificateAuthenticationValidator(
                            new CertificateAuthenticationConfigBuilder()
                                .WithIssuer(SecretProvider, issuerKey)
                                .Build());

                    services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                            .AddSingleton(certificateValidator)
                            .AddControllers(opt => opt.AddCertificateAuthenticationFilter());
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);
                
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
        public async Task CertificateWithDirectValidatorAuthorizedRoute_DoesntEmitSecurityEventsByDefault_RunsAuthentication()
        {
            // Arrange
            const string issuerKey = "issuer";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                            .AddControllers(opt => opt.AddCertificateAuthenticationFilter(auth => auth.WithIssuer(SecretProvider, issuerKey)));
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);
                
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
        public async Task CertificateAuthorizedRoute_EmitsSecurityEventsWhenRequestedOnFilters_RunsAuthentication(bool emitsSecurityEvents)
        {
            // Arrange
            const string issuerKey = "issuer";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    var certificateValidator =
                        new CertificateAuthenticationValidator(
                            new CertificateAuthenticationConfigBuilder()
                                .WithIssuer(SecretProvider, issuerKey)
                                .Build());

                    services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                            .AddSingleton(certificateValidator)
                            .AddMvc(opt => opt.Filters.AddCertificateAuthentication(authOptions =>
                            {
                                authOptions.EmitSecurityEvents = emitsSecurityEvents;
                            }));
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);
                
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CertificateAuthorizedRoute_EmitsSecurityEventsWhenRequested_RunsAuthentication(bool emitsSecurityEvents)
        {
            // Arrange
            const string issuerKey = "issuer";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    var certificateValidator =
                        new CertificateAuthenticationValidator(
                            new CertificateAuthenticationConfigBuilder()
                                .WithIssuer(SecretProvider, issuerKey)
                                .Build());

                    services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                            .AddSingleton(certificateValidator)
                            .AddControllers(opt => opt.AddCertificateAuthenticationFilter(authOptions =>
                            {
                                authOptions.EmitSecurityEvents = emitsSecurityEvents;
                            }));
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);
                
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CertificateWithDirectValidatorAuthorizedRoute_EmitsSecurityEventsWhenRequested_RunsAuthentication(bool emitsSecurityEvents)
        {
            // Arrange
            const string issuerKey = "issuer";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores => stores.AddInMemory(issuerKey, "CN=issuer"))
                            .AddControllers(opt =>
                            {
                                opt.AddCertificateAuthenticationFilter(
                                    auth => auth.WithIssuer(SecretProvider, issuerKey),
                                    authOptions => authOptions.EmitSecurityEvents = emitsSecurityEvents);
                            });
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(NoneAuthenticationController.GetRoute);
                
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
