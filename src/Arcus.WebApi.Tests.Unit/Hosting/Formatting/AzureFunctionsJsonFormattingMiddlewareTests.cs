using System.Net;
using System.Text;
using System.Threading.Tasks;
using Arcus.WebApi.Hosting.AzureFunctions.Formatting;
using Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions;
using Bogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Hosting.Formatting
{
    public class AzureFunctionsJsonFormattingMiddlewareTests
    {
        private static readonly Faker BogusGenerator = new Faker();

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

        [Fact]
        public async Task Request_WithAllAllowAndJsonContentTypeHeaders_ReturnsOk()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var contents = Encoding.UTF8.GetBytes("Something to write so that we require a Content-Type");
            var context = TestFunctionContext.Create(req =>
            {
                req.Body.Write(contents, 0, contents.Length);
                req.Headers.Add("content-type", "application/json");
                req.Headers.TryAddWithoutValidation("allow", "*/*");
            });

            // Act
            await middleware.Invoke(context, CreateOkResponse);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithJsonAllowHeaderWithExtension_ReturnsOk()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var weight = BogusGenerator.Random.Double();
            var context = TestFunctionContext.Create(req =>
            {
                req.Headers.TryAddWithoutValidation("allow", $"application/json, q={weight}");
            });

            // Act
            await middleware.Invoke(context, CreateOkResponse);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Request_WithAllAllowHeaderWithExtension_ReturnsOk()
        {
            // Arrange
            var middleware = new AzureFunctionsJsonFormattingMiddleware();
            var context = TestFunctionContext.Create(req =>
            {
                req.Headers.TryAddWithoutValidation("allow", "q=0.8, */*");
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
