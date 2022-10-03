using System.Net;
using System.Threading.Tasks;
using Arcus.WebApi.Hosting.AzureFunctions.Formatting;
using Arcus.WebApi.Logging.AzureFunctions;
using Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Hosting.Formatting
{
    public class AzureFunctionsJsonFormattingMiddlewareTests
    {
        [Fact]
        public async Task Request_WithoutJsonFormattingHeaders_ReturnsFailure()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var context = TestFunctionContext.Create();

            // Act
            await middleware.Invoke(context, ctx => Task.CompletedTask);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithoutContentTypeHeader_ReturnsFailure()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var context = TestFunctionContext.Create(req => req.Headers.TryAddWithoutValidation("allow", "application/json"));

            // Act
            await middleware.Invoke(context, ctx => Task.CompletedTask);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithoutAllowHeader_ReturnsFailure()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var context = TestFunctionContext.Create(req => req.Headers.Add("content-Type", "application/json"));

            // Act
            await middleware.Invoke(context, ctx => Task.CompletedTask);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithJsonAllowAndContentTypeHeaders_ReturnsFailure()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var context = TestFunctionContext.Create(req =>
            {
                req.Headers.Add("content-type", "application/json");
                req.Headers.TryAddWithoutValidation("allow", "application/json");
            });

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                HttpRequestData request = await ctx.GetHttpRequestDataAsync();
                ctx.GetInvocationResult().Value = request.CreateResponse(HttpStatusCode.OK);
            });

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
