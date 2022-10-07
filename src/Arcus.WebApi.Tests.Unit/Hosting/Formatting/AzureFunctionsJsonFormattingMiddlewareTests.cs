using System.Net;
using System.Text;
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
            var contents = Encoding.UTF8.GetBytes("Something to write so that we require a Content-Type");
            var context = TestFunctionContext.Create(req => req.Body.Write(contents, 0, contents.Length));

            // Act
            await middleware.Invoke(context, CreateOkResponse);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithWrongContentType_ReturnsFailure()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var contents = Encoding.UTF8.GetBytes("Something to write so that we require a Content-Type");
            var context = TestFunctionContext.Create(req =>
            {
                req.Body.Write(contents, 0, contents.Length);
                req.Headers.TryAddWithoutValidation("content-type", "text/plain");
            });

            // Act
            await middleware.Invoke(context, CreateOkResponse);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithoutContentTypeHeader_ReturnsFailure()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var contents = Encoding.UTF8.GetBytes("Something to write so that we require a Content-Type");
            var context = TestFunctionContext.Create(req =>
            {
                req.Body.Write(contents, 0, contents.Length);
                req.Headers.TryAddWithoutValidation("allow", "application/json");
            });

            // Act
            await middleware.Invoke(context, CreateOkResponse);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithWrongAllowHeader_ReturnsFailure()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var contents = Encoding.UTF8.GetBytes("Something to write so that we require a Content-Type");
            var context = TestFunctionContext.Create(req =>
            {
                req.Body.Write(contents, 0, contents.Length);
                req.Headers.Add("content-Type", "application/json");
                req.Headers.Add("accept", "text/plain");
            });

            // Act
            await middleware.Invoke(context, CreateOkResponse);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithJsonAllowAndContentTypeHeaders_ReturnsOk()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var contents = Encoding.UTF8.GetBytes("Something to write so that we require a Content-Type");
            var context = TestFunctionContext.Create(req =>
            {
                req.Body.Write(contents, 0, contents.Length);
                req.Headers.Add("content-type", "application/json");
                req.Headers.TryAddWithoutValidation("allow", "application/json");
            });

            // Act
            await middleware.Invoke(context, CreateOkResponse);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static async Task CreateOkResponse(FunctionContext context)
        {
            HttpRequestData request = await context.GetHttpRequestDataAsync();
            context.GetInvocationResult().Value = request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
