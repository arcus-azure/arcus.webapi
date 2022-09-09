using System;
using System.Net;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Logging.AzureFunctions;
using Arcus.WebApi.Tests.Unit.Logging.Fixture;
using Bogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class AzureFunctionsExceptionHandlingTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public async Task ExceptionHandlingMiddleware_WithFailure_HandlesException()
        {
            // Arrange
            var spyLogger = new InMemoryLogger();
            var context = TestFunctionContext.Create(
                configureServices: services => services.AddLogging(logging => logging.AddProvider(new CustomLoggerProvider(spyLogger))));

            var middleware = new AzureFunctionsExceptionHandlingMiddleware();

            // Act
            await middleware.Invoke(context, ctx => throw new InvalidOperationException("Sabotage this!"));

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains(spyLogger.Messages, msg => msg.Contains("Sabotage this!"));
        }

        [Fact]
        public async Task ExceptionHandlingMiddleware_WithCustomFailure_HandlesException()
        {
            // Arrange
            var spyLogger = new InMemoryLogger();
            var context = TestFunctionContext.Create(
                configureServices: services => services.AddLogging(logging => logging.AddProvider(new CustomLoggerProvider(spyLogger))));

            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var middleware = new CustomExceptionHandlingWorkerMiddleware(statusCode);

            // Act
            await middleware.Invoke(context, ctx => throw new InvalidOperationException("Sabotage this!"));

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(statusCode, response.StatusCode);
            Assert.Contains(spyLogger.Messages, msg => msg.Contains("Custom exception handling message"));
        }
    }
}
