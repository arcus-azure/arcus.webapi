using Arcus.WebApi.OpenApi.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Unit.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Unit.OpenApi
{
    public class OAuthAuthorizeOperationFilterTests : IDisposable
    {
        private readonly ITestOutputHelper _outputWriter;
        private readonly TestApiServer _testServer = new TestApiServer();

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthAuthorizeOperationFilterTests"/> class.
        /// </summary>
        public OAuthAuthorizeOperationFilterTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Theory]
        [InlineData(new object[] { new[] { "valid scope", "" } })]
        [InlineData(new object[] { new[] { "valid scope", null, "another scope" } })]
        public void OAuthAuthorizeOperationFilter_ShouldFailWithInvalidScopeList(IEnumerable<string> scopes)
        {
            Assert.Throws<ArgumentException>(() => new OAuthAuthorizeOperationFilter(scopes));
        }

        [Fact]
        public async Task OAuthAuthorizeOperationFilter_ShouldIncludeSecurityDefinitionResponses_OnAuthorizedOperations()
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
                        swagger.Paths.TryGetValue("/oauth/authorize", out OpenApiPathItem oauthPath), 
                        "Cannot find OAuth authorized path in Open API spec file");

                    Assert.True(
                        oauthPath.Operations.TryGetValue(OperationType.Get, out OpenApiOperation oauthOperation),
                        "Cannot find OAuth GET operation in Open API spec file");

                    OpenApiResponses oauthResponses = oauthOperation.Responses;
                    Assert.Contains(oauthResponses, r => r.Key == "401");
                    Assert.Contains(oauthResponses, r => r.Key == "403");
                }
            }
        }

        [Fact]
        public async Task OAuthAuthorizeOperationFilter_ShouldNotIncludeSecurityDefinitionResponses_OnNonAuthorizedOperations()
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
                        swagger.Paths.TryGetValue("/oauth/none", out OpenApiPathItem oauthPath), 
                        "Cannot find OAuth none authorized path in Open API spec file");

                    Assert.True(
                        oauthPath.Operations.TryGetValue(OperationType.Get, out OpenApiOperation oauthOperation),
                        "Cannot find OAuth GET operation in Open API spec file");

                    OpenApiResponses oauthResponses = oauthOperation.Responses;
                    Assert.DoesNotContain(oauthResponses, r => r.Key == "401");
                    Assert.DoesNotContain(oauthResponses, r => r.Key == "403");
                }
            }
        }

        // TODO: should we add a test to check if we also include the security definitions when we add one of our authentication filters?

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _testServer?.Dispose();
        }
    }
}
