using Arcus.WebApi.OpenApi.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Unit.Hosting;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.WebApi.Unit.OpenApi
{
    public class OAuthAuthorizeOperationFilterTests : IDisposable
    {
        private readonly TestApiServer _testServer = new TestApiServer();

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

                /* TODO:
                    use 'OpenApiStreamReader' to load response content into a 'OpenApiDocument'
                    this will require a change in the 'OAuthAuthorizeOperationFilter' since now the generated JSON document is invalid according to the Open API specs:
                    -> Invalid Reference identifier 'oauth2'., The key 'KeyValuePair[String,IEnumerable[String]]' in 'schemas' of components MUST match the regular expression '^[a-zA-Z0-9\.\-_]+$'. */

                string swaggerJson = await response.Content.ReadAsStringAsync();
                JObject swagger = JObject.Parse(swaggerJson);
                var responses = swagger["paths"]["/oauth/authorize"]["get"]["responses"].Children<JProperty>();
                
                Assert.Contains(responses, r => r.Name == "401");
                Assert.Contains(responses, r => r.Name == "403");
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

                /* TODO:
                    use 'OpenApiStreamReader' to load response content into a 'OpenApiDocument'
                    this will require a change in the 'OAuthAuthorizeOperationFilter' since now the generated JSON document is invalid according to the Open API specs:
                    -> Invalid Reference identifier 'oauth2'., The key 'KeyValuePair[String,IEnumerable[String]]' in 'schemas' of components MUST match the regular expression '^[a-zA-Z0-9\.\-_]+$'. */

                string swaggerJson = await response.Content.ReadAsStringAsync();
                JObject swagger = JObject.Parse(swaggerJson);
                var responses = swagger["paths"]["/oauth/none"]["get"]["responses"].Children<JProperty>();
                
                Assert.DoesNotContain(responses, r => r.Name == "401");
                Assert.DoesNotContain(responses, r => r.Name == "403");
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
