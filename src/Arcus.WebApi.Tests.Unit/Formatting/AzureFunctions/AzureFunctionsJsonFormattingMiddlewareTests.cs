using System.Net;
using System.Threading.Tasks;
using Arcus.WebApi.Hosting.AzureFunctions.Formatting;
using Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Formatting.AzureFunctions
{
    public class AzureFunctionsJsonFormattingMiddlewareTests
    {
        [Fact]
        public async Task Invoke_WithoutBodyAndWithoutAccept_ByPass()
        {
            // Arrange
            var context = TestFunctionContext.Create();
            var middleware = new AzureFunctionsJsonFormattingMiddleware();

            // Act
            await middleware.Invoke(context, CreateOkResponseAsync);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WithoutBodyAndWithInvalidAccept_Fails()
        {
            // Arrange
            var context = TestFunctionContext.Create(ctx =>
            {
                var req = TestHttpRequestData.Generate(ctx);
                req.Headers.Add("Accept", "text/plain");
                return req;
            });
            
            var middleware = new AzureFunctionsJsonFormattingMiddleware();

            // Act
            await middleware.Invoke(context, CreateOkResponseAsync);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WithInvalidContentTypeWithoutAccept_Fails()
        {
            // Arrange
            var context = TestFunctionContext.Create(ctx =>
            {
                var req = TestHttpRequestData.Generate(ctx);
                req.Body.WriteByte(0);
                req.Headers.Add("Content-Type", "text/plain");
                return req;
            });
            
            var middleware = new AzureFunctionsJsonFormattingMiddleware();

            // Act
            await middleware.Invoke(context, CreateOkResponseAsync);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WithInvalidContentTypeWithInvalidAccept_Fails()
        {
            // Arrange
            var context = TestFunctionContext.Create(ctx =>
            {
                var req = TestHttpRequestData.Generate(ctx);
                req.Body.WriteByte(0);
                req.Headers.Add("Content-Type", "text/plain");
                req.Headers.Add("Accept", "text/plain");
                return req;
            });
            
            var middleware = new AzureFunctionsJsonFormattingMiddleware();

            // Act
            await middleware.Invoke(context, CreateOkResponseAsync);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WithValidContentTypeWithValidAccept_Fails()
        {
            // Arrange
            var context = TestFunctionContext.Create(ctx =>
            {
                var req = TestHttpRequestData.Generate(ctx);
                req.Body.WriteByte(0);
                req.Headers.Add("Content-Type", "application/json");
                req.Headers.Add("Accept", "application/json");
                return req;
            });
            
            var middleware = new AzureFunctionsJsonFormattingMiddleware();

            // Act
            await middleware.Invoke(context, CreateOkResponseAsync);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static async Task CreateOkResponseAsync(FunctionContext ctx)
        {
            HttpRequestData req = await ctx.GetHttpRequestDataAsync();
            Assert.NotNull(req);
            ctx.GetInvocationResult().Value = req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
