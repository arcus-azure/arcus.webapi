using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.OpenApi.Extensions;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.OpenApi.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.OpenApi
{
    [Collection("Integration")]
    public class AuthenticationOperationFilterTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationOperationFilterTests"/> class.
        /// </summary>
        public AuthenticationOperationFilterTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Theory]
        [InlineData(AuthenticationController.OAuthRoute)]
        [InlineData(AuthenticationController.SharedAccessKeyRoute)]
        [InlineData(AuthenticationController.CertificateRoute)]
        public async Task AuthenticationOperationFilter_ShouldIncludeSecurityDefinitionResponses_OnAuthorizedOperations(string authorizedRoute)
        {
            // Arrange
            string assemblyName = typeof(AuthenticationOperationFilterTests).Assembly.GetName().Name;
            var options = new ServerOptions()
                .ConfigureServices(services =>
                {
                    var openApiInformation = new OpenApiInfo {Title = assemblyName, Version = "v1"};
                    services.AddSwaggerGen(swagger =>
                    {
                        swagger.SwaggerDoc("v1", openApiInformation);
                        swagger.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, assemblyName + ".Open-Api.xml"));
                        swagger.OperationFilter<OAuthAuthorizeOperationFilter>(new object[] { new[] { "myApiScope" } });
                        swagger.OperationFilter<SharedAccessKeyAuthenticationOperationFilter>("sharedaccesskey", SecuritySchemeType.ApiKey);
                        swagger.OperationFilter<CertificateAuthenticationOperationFilter>("certificate", SecuritySchemeType.ApiKey);
                    });
                })
                .Configure(app =>
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(swagger =>
                    {
                        swagger.SwaggerEndpoint("v1/swagger.json", assemblyName);
                        swagger.DocumentTitle = assemblyName;
                    });
                });
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get("/swagger/v1/swagger.json");
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    OpenApiDocument swagger = await ReadSwaggerOpenAPiDocumentAsync(response);
                    Assert.True(swagger.Paths.TryGetValue("/" + authorizedRoute, out OpenApiPathItem path),
                        $"Cannot find /{authorizedRoute} authorized path in Open API spec file");

                    Assert.True(path.Operations.TryGetValue(OperationType.Get, out OpenApiOperation operation),
                        "Cannot find Shared Access Key GET operation in Open API spec file");

                    OpenApiResponses operationResponses = operation.Responses;
                    Assert.Contains(operationResponses, r => r.Key == "401");
                    Assert.Contains(operationResponses, r => r.Key == "403");
                }
            }
        }

        [Fact]
        public async Task AuthenticationOperationFilter_ShouldNotIncludeSecurityDefinitionResponses_OnNonAuthorizedOperations()
        {
            // Arrange
             string assemblyName = typeof(AuthenticationOperationFilterTests).Assembly.GetName().Name;
            var options = new ServerOptions()
                .ConfigureServices(services =>
                {
                    var openApiInformation = new OpenApiInfo {Title = assemblyName, Version = "v1"};
                    services.AddSwaggerGen(swagger =>
                    {
                        swagger.SwaggerDoc("v1", openApiInformation);
                        swagger.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, assemblyName + ".Open-Api.xml"));
                        swagger.OperationFilter<OAuthAuthorizeOperationFilter>(new object[] { new[] { "myApiScope" } });
                        swagger.OperationFilter<SharedAccessKeyAuthenticationOperationFilter>("sharedaccesskey", SecuritySchemeType.ApiKey);
                        swagger.OperationFilter<CertificateAuthenticationOperationFilter>("certificate", SecuritySchemeType.ApiKey);
                    });
                })
                .Configure(app =>
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(swagger =>
                    {
                        swagger.SwaggerEndpoint("v1/swagger.json", assemblyName);
                        swagger.DocumentTitle = assemblyName;
                    });
                });
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get("/swagger/v1/swagger.json");
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    OpenApiDocument swagger = await ReadSwaggerOpenAPiDocumentAsync(response);
                    
                    Assert.True(swagger.Paths.TryGetValue("/" + AuthenticationController.NoneRoute, out OpenApiPathItem path),
                        $"Cannot find /{AuthenticationController.NoneRoute} none authorized path in Open API spec file");

                    Assert.True(path.Operations.TryGetValue(OperationType.Get, out OpenApiOperation operation),
                        "Cannot find GET operation in Open API spec file");

                    OpenApiResponses operationResponses = operation.Responses;
                    Assert.DoesNotContain(operationResponses, r => r.Key == "401");
                    Assert.DoesNotContain(operationResponses, r => r.Key == "403");
                }
            }
        }

        private async Task<OpenApiDocument> ReadSwaggerOpenAPiDocumentAsync(HttpResponseMessage response)
        {
            var reader = new OpenApiStreamReader();
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                OpenApiDocument swagger = reader.Read(responseStream, out OpenApiDiagnostic diagnostic);
                _logger.LogTrace(diagnostic.Errors.Count == 0
                    ? String.Empty
                    : String.Join(", ", diagnostic.Errors.Select(e => e.Message + ": " + e.Pointer)));

                return swagger;
            }
        }
    }
}
