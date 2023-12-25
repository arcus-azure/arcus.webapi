using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.AzureFunctions;
using Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions;
using Bogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class AzureFunctionsRequestTrackingTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public async Task SendRequest_WithDefaultOptions_TracksRequest()
        {
            // Arrange
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware();

            var pending = PendingHttpRequestData.Generate();
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx => await CreateHttpResponse(ctx, status));

            // Assert
            AssertHttpTrackedRequest(spyLogger, pending, status);
        }

        [Fact]
        public async Task SendRequest_WithOmittedRoute_IgnoresRequest()
        {
            // Arrange
            string url = BogusGenerator.Internet.UrlWithPath();
            string route = new Uri(url).AbsolutePath;
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options => options.OmittedRoutes.Add(route));

            var pending = PendingHttpRequestData.Generate(url: url);
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx => await CreateHttpResponse(ctx, status));

            // Assert
            string message = Assert.Single(spyLogger.Messages);
            Assert.Contains("Skip request tracking", message);
        }

        [Fact]
        public async Task SendRequest_WithOmittedHeader_IgnoresHeader()
        {
            // Arrange
            string headerName = BogusGenerator.Random.Guid().ToString();
            string headerValue = BogusGenerator.Random.Guid().ToString();
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options => options.OmittedHeaderNames.Add(headerName));

            var pending = PendingHttpRequestData.Generate(headerName: headerName, headerValue: headerValue);
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx => await CreateHttpResponse(ctx, status));

            // Assert
            LogEntry trackedRequestEntry = Assert.Single(spyLogger.Entries, entry => entry.Message.Contains(pending.Url.PathAndQuery));
            Assert.Contains(pending.Method, trackedRequestEntry.Message);
            Assert.Contains(((int) status).ToString(), trackedRequestEntry.Message);
            Assert.DoesNotContain(headerName, trackedRequestEntry.Message);
            Assert.DoesNotContain(headerValue, trackedRequestEntry.Message);
        }

        [Fact]
        public async Task SendRequest_WithCustomOmittedHeader_AddsCustomHeader()
        {
            // Arrange
            var middleware = new CustomRequestTrackingMiddleware(new RequestTrackingOptions());

            var pending = PendingHttpRequestData.Generate();
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx => await CreateHttpResponse(ctx, status));

            // Assert
            string message = AssertHttpTrackedRequest(spyLogger, pending, status);
            Assert.Contains("x-custom-key", message);
            Assert.Contains("x-custom-value", message);
        }

        [Fact]
        public async Task SendRequest_WithDisableTrackingHeaders_IgnoresAllHeaders()
        {
            // Arrange
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options => options.IncludeRequestHeaders = false);

            var pending = PendingHttpRequestData.Generate();
            var status = BogusGenerator.PickRandom<HttpStatusCode>();
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);

            // Act
            await middleware.Invoke(context, async ctx => await CreateHttpResponse(ctx, status));

            // Assert
            LogEntry trackedRequestEntry = Assert.Single(spyLogger.Entries, entry => entry.Message.Contains(pending.Url.PathAndQuery));
            Assert.Contains(pending.Method, trackedRequestEntry.Message);
            Assert.Contains(((int) status).ToString(), trackedRequestEntry.Message);
            Assert.All(pending.Headers, header =>
            {
                Assert.DoesNotContain(header.Key, trackedRequestEntry.Message);
                Assert.DoesNotContain(header.Value.FirstOrDefault(), trackedRequestEntry.Message);
            });
        }

        [Fact]
        public async Task SendRequest_WithOmittedStatusCode_IgnoresRequest()
        {
            // Arrange
            var notTrackedStatusCode = HttpStatusCode.BadGateway;
            HttpStatusCode status = BogusGenerator.PickRandomWithout(notTrackedStatusCode);
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options => options.TrackedStatusCodes.Add(status));

            var pending = PendingHttpRequestData.Generate();
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);

            // Act
            await middleware.Invoke(context, async ctx => await CreateHttpResponse(ctx, notTrackedStatusCode));

            // Assert
            Assert.Contains(spyLogger.Messages, msg => msg.Contains("Request tracking for this endpoint is disallowed as the response status code"));
        }

        [Fact]
        public async Task SendRequest_WithOmittedStatusCodeRange_IgnoresRequest()
        {
            // Arrange
            var status = (HttpStatusCode) BogusGenerator.Random.Int(min: 200, max: 300);
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options => options.TrackedStatusCodeRanges.Add(new StatusCodeRange(300, 500)));

            var pending = PendingHttpRequestData.Generate();
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);

            // Act
            await middleware.Invoke(context, async ctx => await CreateHttpResponse(ctx, status));

            // Assert
            Assert.Contains(spyLogger.Messages, msg => msg.Contains("Request tracking for this endpoint is disallowed as the response status code"));
        }

        [Fact]
        public async Task SendRequest_WithTrackedRequestBody_IncludesRequestBody()
        {
            // Arrange
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options => options.IncludeRequestBody = true);

            string contents = BogusGenerator.Lorem.Sentence();
            var requestBody = new ReadOnceStream(new MemoryStream(Encoding.UTF8.GetBytes(contents)));
            var pending = PendingHttpRequestData.Generate(body: requestBody);
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                HttpRequestData request = await ctx.GetHttpRequestDataAsync();
                string actual = await request.ReadAsStringAsync();
                Assert.Equal(contents, actual);

                await CreateHttpResponse(ctx, status);
            });

            // Assert
            string message = AssertHttpTrackedRequest(spyLogger, pending, status);
            Assert.Contains(contents, message);
        }

        [Fact]
        public async Task SendRequest_WithTrackedPartRequestBody_IncludesRequestBody()
        {
            // Arrange
            var bufferSize = 100;
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options =>
            {
                options.IncludeRequestBody = true;
                options.RequestBodyBufferSize = bufferSize;
            });

            string contents = BogusGenerator.Lorem.Sentence(wordCount: 1000);
            var requestBody = new ReadOnceStream(new MemoryStream(Encoding.UTF8.GetBytes(contents)));
            var pending = PendingHttpRequestData.Generate(body: requestBody);
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                HttpRequestData request = await ctx.GetHttpRequestDataAsync();
                string actual = await request.ReadAsStringAsync();
                Assert.Equal(contents, actual);

                await CreateHttpResponse(ctx, status);
            });

            // Assert
            string message = AssertHttpTrackedRequest(spyLogger, pending, status);
            Assert.DoesNotContain(contents, message);

            using (var reader = new StringReader(contents))
            {
                var buffer = new char[bufferSize];
                reader.ReadBlock(buffer, 0, buffer.Length);

                var partyRequestBody = new string(buffer);
                Assert.Contains(partyRequestBody, message);
            }
        }

        [Fact]
        public async Task SendRequest_WithCustomRequestBodySanitization_IncludesRequestBody()
        {
            // Arrange
            var middleware = new CustomRequestTrackingMiddleware(new RequestTrackingOptions { IncludeRequestBody = true });

            string contents = BogusGenerator.Lorem.Sentence();
            var requestBody = new ReadOnceStream(new MemoryStream(Encoding.UTF8.GetBytes(contents)));
            var pending = PendingHttpRequestData.Generate(body: requestBody);
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                HttpRequestData request = await ctx.GetHttpRequestDataAsync();
                string actual = await request.ReadAsStringAsync();
                Assert.Equal(contents, actual);

                await CreateHttpResponse(ctx, status);
            });

            // Assert
            string message = AssertHttpTrackedRequest(spyLogger, pending, status);
            Assert.Contains($"custom[{contents}]", message);
        }

        [Fact]
        public async Task SendRequest_WithTrackedResponseBody_IncludesResponseBody()
        {
            // Arrange
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options => options.IncludeResponseBody = true);

            string contents = BogusGenerator.Lorem.Sentence();
            var pending = PendingHttpRequestData.Generate();
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                HttpRequestData request = await ctx.GetHttpRequestDataAsync();
                HttpResponseData response = request.CreateResponse(status);
                response.WriteString(contents);

                ctx.GetInvocationResult().Value = response;
            });

            // Assert
            string message = AssertHttpTrackedRequest(spyLogger, pending, status);
            Assert.Contains(contents, message);
            object result = context.GetInvocationResult().Value;
            Assert.NotNull(result);
            var response = Assert.IsType<TestHttpResponseData>(result);

            response.Body.Position = 0;
            using (var reader = new StreamReader(response.Body))
            {
                string expected = await reader.ReadToEndAsync();
                Assert.Equal(contents, expected);
            }
        }

        [Fact]
        public async Task SendRequest_WithCustomRequestBodySanitization_IncludesResponseBody()
        {
            // Arrange
            var middleware = new CustomRequestTrackingMiddleware(new RequestTrackingOptions { IncludeResponseBody = true });

            string contents = BogusGenerator.Lorem.Sentence();
            var pending = PendingHttpRequestData.Generate();
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                HttpRequestData request = await ctx.GetHttpRequestDataAsync();
                HttpResponseData response = request.CreateResponse(status);
                response.WriteString(contents);

                ctx.GetInvocationResult().Value = response;
            });

            // Assert
            string message = AssertHttpTrackedRequest(spyLogger, pending, status);
            Assert.Contains($"custom[{contents}", message);
            object result = context.GetInvocationResult().Value;
            Assert.NotNull(result);
            var response = Assert.IsType<TestHttpResponseData>(result);

            response.Body.Position = 0;
            using (var reader = new StreamReader(response.Body))
            {
                string expected = await reader.ReadToEndAsync();
                Assert.Equal(contents, expected);
            }
        }

        [Fact]
        public async Task SendRequest_WithTrackedPartResponseBody_IncludesResponseBody()
        {
            // Arrange
            var bufferSize = 100;
            AzureFunctionsRequestTrackingMiddleware middleware = CreateMiddleware(options =>
            {
                options.IncludeResponseBody = true;
                options.ResponseBodyBufferSize = bufferSize;
            });

            string contents = BogusGenerator.Lorem.Sentence(wordCount: 1000);
            var pending = PendingHttpRequestData.Generate();
            var spyLogger = new InMemoryLogger();
            FunctionContext context = CreateFunctionContext(pending, spyLogger);
            var status = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                HttpRequestData request = await ctx.GetHttpRequestDataAsync();
                HttpResponseData response = request.CreateResponse(status);
                response.WriteString(contents);

                ctx.GetInvocationResult().Value = response;
            });

            // Assert
            string message = AssertHttpTrackedRequest(spyLogger, pending, status);
            object result = context.GetInvocationResult().Value;
            Assert.NotNull(result);
            var response = Assert.IsType<TestHttpResponseData>(result);

            response.Body.Position = 0;
            using (var reader = new StreamReader(response.Body))
            {
                string expected = await reader.ReadToEndAsync();
                Assert.Equal(contents, expected);
            }

            Assert.DoesNotContain(contents, message);
            using (var reader = new StringReader(contents))
            {
                var buffer = new char[bufferSize];
                reader.ReadBlock(buffer, 0, buffer.Length);
                var expected = new string(buffer);
                Assert.Contains(expected, message);
            }
        }

        private static AzureFunctionsRequestTrackingMiddleware CreateMiddleware(Action<RequestTrackingOptions> configureOptions = null)
        {
            var options = new RequestTrackingOptions();
            configureOptions?.Invoke(options);

            return new AzureFunctionsRequestTrackingMiddleware(options);
        }

        private static FunctionContext CreateFunctionContext(
            PendingHttpRequestData pending,
            InMemoryLogger spyLogger)
        {
            var context = TestFunctionContext.Create(
                context => pending.Activate(context), 
                services => services.AddLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Trace)
                           .AddProvider(new CustomLoggerProvider(spyLogger));
                }));

            return context;
        }

        private static async Task CreateHttpResponse(FunctionContext context, HttpStatusCode status)
        {
            HttpRequestData request = await context.GetHttpRequestDataAsync();
            context.GetInvocationResult().Value = request.CreateResponse(status);
        }

        private static string AssertHttpTrackedRequest(
            InMemoryLogger spyLogger,
            PendingHttpRequestData expected,
            HttpStatusCode status)
        {
            LogEntry trackedRequestEntry = Assert.Single(spyLogger.Entries, entry => entry.Message.Contains(expected.Url.PathAndQuery));
            Assert.Contains(expected.Method, trackedRequestEntry.Message);
            Assert.Contains(((int) status).ToString(), trackedRequestEntry.Message);
            Assert.All(expected.Headers, header =>
            {
                Assert.Contains(header.Key, trackedRequestEntry.Message);
                Assert.Contains(string.Join(",", header.Value), trackedRequestEntry.Message);
            });

            return trackedRequestEntry.Message;
        }
    }
}
