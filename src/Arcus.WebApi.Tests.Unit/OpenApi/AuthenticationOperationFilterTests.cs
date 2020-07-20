using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.OpenApi.Extensions;
using Arcus.WebApi.Tests.Unit.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Unit.OpenApi
{
    public class AuthenticationOperationFilterTests : IDisposable
    {
        private readonly ITestOutputHelper _outputWriter;
        private readonly TestApiServer _testServer = new TestApiServer();

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationOperationFilterTests"/> class.
        /// </summary>
        public AuthenticationOperationFilterTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(AuthenticationController.OAuthRoute)]
        [InlineData(AuthenticationController.SharedAccessKeyRoute)]
        [InlineData(AuthenticationController.CertificateRoute)]
        public async Task AuthenticationOperationFilter_ShouldIncludeSecurityDefinitionResponses_OnAuthorizedOperations(string authorizedRoute)
        {
            // Arrange
            using (var client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync("swagger/v1/swagger.json"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var reader = new OpenApiStreamReader();
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    OpenApiDocument swagger = reader.Read(responseStream, out OpenApiDiagnostic diagnostic);
                    _outputWriter.WriteLine(diagnostic.Errors.Count == 0 ? String.Empty : String.Join(", ", diagnostic.Errors.Select(e => e.Message + ": " + e.Pointer)));

                    Assert.True(
                        swagger.Paths.TryGetValue("/" + authorizedRoute, out OpenApiPathItem path),
                        $"Cannot find /{authorizedRoute} authorized path in Open API spec file");

                    Assert.True(
                        path.Operations.TryGetValue(OperationType.Get, out OpenApiOperation operation),
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
            using (var client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync("swagger/v1/swagger.json"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var reader = new OpenApiStreamReader();
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    OpenApiDocument swagger = reader.Read(responseStream, out OpenApiDiagnostic diagnostic);
                    _outputWriter.WriteLine(diagnostic.Errors.Count == 0 ? String.Empty : String.Join(", ", diagnostic.Errors.Select(e => e.Message + ": " + e.Pointer)));

                    Assert.True(
                        swagger.Paths.TryGetValue("/" + AuthenticationController.NoneRoute, out OpenApiPathItem path),
                        $"Cannot find /{AuthenticationController.NoneRoute} none authorized path in Open API spec file");

                    Assert.True(
                        path.Operations.TryGetValue(OperationType.Get, out OpenApiOperation operation),
                        "Cannot find GET operation in Open API spec file");

                    OpenApiResponses operationResponses = operation.Responses;
                    Assert.DoesNotContain(operationResponses, r => r.Key == "401");
                    Assert.DoesNotContain(operationResponses, r => r.Key == "403");
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new[] { "valid scope", "" } })]
        [InlineData(new object[] { new[] { "valid scope", null, "another scope" } })]
        public void OAuthAuthorizeOperationFilter_ShouldFailWithInvalidScopeList(IEnumerable<string> scopes)
        {
            Assert.Throws<ArgumentException>(() => new OAuthAuthorizeOperationFilter(scopes));
        }

        [Fact]
        public void SharedAccessKeyAuthenticationOperationFilter_ShouldFailWithInvalidScopeList()
        {
            Assert.Throws<ArgumentException>(() => new SharedAccessKeyAuthenticationOperationFilter());
        }

        [Fact]
        public void CertificateAuthenticationOperationFilter_ShouldFailWithInvalidScopeList()
        {
            Assert.Throws<ArgumentException>(() => new CertificateAuthenticationOperationFilter());
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _testServer?.Dispose();
        }
    }
}
