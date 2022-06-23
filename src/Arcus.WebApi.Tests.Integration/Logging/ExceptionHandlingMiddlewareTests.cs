using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Controllers;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Trait("Category", "Integration")]
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddlewareTests" /> class.
        /// </summary>
        public ExceptionHandlingMiddlewareTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task UseExceptionHandling_WithControllerFailure_CatchesException()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseExceptionHandling())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(SabotageController.Route);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                    IEnumerable<LogEvent> logEvents = spySink.DequeueLogEvents();
                    Assert.Contains(logEvents, logEvent => logEvent.RenderMessage().Contains("sabotage", StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        [Fact]
        public async Task UseCustomExceptionHandling_WithControllerFailure_CatchesException()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseExceptionHandling<NonInternalServerErrorExceptionHandlingMiddleware>())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(SabotageController.Route);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Arrange
                    Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
                    IEnumerable<LogEvent> logEvents = spySink.DequeueLogEvents();
                    Assert.Contains(logEvents, logEvent => logEvent.RenderMessage().Contains("Testing", StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }
}
