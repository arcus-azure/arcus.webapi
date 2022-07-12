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
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
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
    [Collection("Integration")]
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
                                    .WithSubject(X509ValidationLocation.SecretProvider, subjectKey)
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
                                    .WithSubject(X509ValidationLocation.SecretProvider, subjectKey)
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
                                    .WithThumbprint(X509ValidationLocation.SecretProvider, thumbprintKey)
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
                                    .WithThumbprint(X509ValidationLocation.SecretProvider, thumbprintKey)
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
                                    .WithSubject(X509ValidationLocation.SecretProvider, subjectKey)
                                    .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
                                    .WithSubject(X509ValidationLocation.SecretProvider, subjectKey)
                                    .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
                                    .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                                    .WithIssuer(X509ValidationLocation.Configuration, issuerKey)
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
                                    .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                                    .WithIssuer(X509ValidationLocation.Configuration, issuerKey)
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
                                    .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                                    .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
                                    .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                                    .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
                                .WithSubject(X509ValidationLocation.Configuration, "ignored-subject")
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
                                .WithSubject(X509ValidationLocation.Configuration, "ignored-subject")
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
                                    .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                                    .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
                                    .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                                    .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
                                    .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
                                    .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
        
        [Fact]
        public async Task SharedAccessKeyAuthorizedRoute_DoesntEmitSecurityEventsByDefaultOnFilters_RunsAuthentication()
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
                                .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
        public async Task SharedAccessKeyAuthorizedRoute_DoesntEmitSecurityEventsByDefault_RunsAuthentication()
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
                                .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SharedAccessKeyAuthorizedRoute_EmitsSecurityEventsWhenRequestedOnFilters_RunsAuthentication(bool emitsSecurityEvents)
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
                                .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
        public async Task SharedAccessKeyAuthorizedRoute_EmitsSecurityEventsWhenRequested_RunsAuthentication(bool emitsSecurityEvents)
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
                                .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
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
    }
}
