using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Testing.Logging;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Unit.Logging.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class HttpMessageHandlerTests : HttpCorrelationTrackingTests
    {
        [Fact]
        public async Task Send_Default_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlation);
            var logger = new InMemoryLogger<HttpCorrelationMessageHandler>();
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, HttpCorrelationProperties.UpstreamServiceHeaderName);
            });

            var services = new ServiceCollection();
            var defaultHttpClientName = string.Empty;
            services.AddSingleton<IHttpCorrelationInfoAccessor>(provider => accessor)
                    .AddSingleton<ILogger<HttpCorrelationMessageHandler>>(provider => logger)
                    .AddHttpClient(defaultHttpClientName)
                    .WithHttpCorrelationTracking()
                    .AddHttpMessageHandler(() => assertion);

            IServiceProvider provider = services.BuildServiceProvider();
            HttpClient client = CreateHttpClient(provider, statusCode);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task Send_WithCustomTransactionIdHeader_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlation);
            var logger = new InMemoryLogger<HttpCorrelationMessageHandler>();
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            string transactionIdHeaderName = "X-MyTransaction-Id";
            var assertion = new AssertHttpMessageHandler(req =>
            {
                // Assert
                AssertHeaderValue(req, transactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, HttpCorrelationProperties.UpstreamServiceHeaderName);
            });

            var services = new ServiceCollection();
            var defaultHttpClientName = string.Empty;
            services.AddSingleton<IHttpCorrelationInfoAccessor>(provider => accessor)
                    .AddSingleton<ILogger<HttpCorrelationMessageHandler>>(provider => logger)
                    .AddHttpClient(defaultHttpClientName)
                    .WithHttpCorrelationTracking(options => options.TransactionIdHeaderName = transactionIdHeaderName)
                    .AddHttpMessageHandler(() => assertion);

            IServiceProvider provider = services.BuildServiceProvider();
            HttpClient client = CreateHttpClient(provider, statusCode);
            HttpRequestMessage request = GenerateHttpRequestMessage();
            
            // Act
            await client.SendAsync(request);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task Send_WithCustomUpstreamServiceHeader_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlation);
            var logger = new InMemoryLogger<HttpCorrelationMessageHandler>();
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            string upstreamServiceHeaderName = "X-MyRequest-Id";
            var assertion = new AssertHttpMessageHandler(req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, upstreamServiceHeaderName);
            });

            var services = new ServiceCollection();
            var defaultHttpClientName = string.Empty;
            services.AddSingleton<IHttpCorrelationInfoAccessor>(provider => accessor)
                    .AddSingleton<ILogger<HttpCorrelationMessageHandler>>(provider => logger)
                    .AddHttpClient(defaultHttpClientName)
                    .WithHttpCorrelationTracking(options => options.UpstreamServiceHeaderName= upstreamServiceHeaderName)
                    .AddHttpMessageHandler(() => assertion);

            IServiceProvider provider = services.BuildServiceProvider();
            HttpClient client = CreateHttpClient(provider, statusCode);
            HttpRequestMessage request = GenerateHttpRequestMessage();
            
            // Act
            await client.SendAsync(request);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task Send_WithCustomDependencyId_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlation);
            var logger = new InMemoryLogger<HttpCorrelationMessageHandler>();
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var dependencyId = $"parent-{Guid.NewGuid()}";
            var assertion = new AssertHttpMessageHandler(req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderValue(req, HttpCorrelationProperties.UpstreamServiceHeaderName, dependencyId);
            });

            var services = new ServiceCollection();
            var defaultHttpClientName = string.Empty;
            services.AddSingleton<IHttpCorrelationInfoAccessor>(provider => accessor)
                    .AddSingleton<ILogger<HttpCorrelationMessageHandler>>(provider => logger)
                    .AddHttpClient(defaultHttpClientName)
                    .WithHttpCorrelationTracking(options => options.GenerateDependencyId= () => dependencyId)
                    .AddHttpMessageHandler(() => assertion);

            IServiceProvider provider = services.BuildServiceProvider();
            HttpClient client = CreateHttpClient(provider, statusCode);
            HttpRequestMessage request = GenerateHttpRequestMessage();
            
            // Act
            await client.SendAsync(request);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, dependencyId, statusCode);
        }

        private static HttpClient CreateHttpClient(IServiceProvider provider, HttpStatusCode statusCode)
        {
            var options = provider.GetService<IOptions<HttpClientFactoryOptions>>();
            Assert.NotNull(options);

            var builder = new DefaultHttpMessageHandlerBuilder(provider)
            {
                PrimaryHandler = new StubHttpMessageHandler(statusCode)
            };
            Assert.All(options.Value.HttpMessageHandlerBuilderActions, action => action(builder));

            HttpMessageHandler handler = builder.Build();
            var client = new HttpClient(handler);
            
            return client;
        }

        [Fact]
        public void Create_WithoutHttpContextAccessor_Fails()
        {
            // Arrange
            var options = new HttpCorrelationClientOptionsTests();
            var logger = NullLogger<HttpCorrelationMessageHandler>.Instance;

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new HttpCorrelationMessageHandler(correlationInfoAccessor: null, options, logger));
        }

        [Fact]
        public void Create_WithoutOptions_Fails()
        {
            // Arrange
            var accessor = Mock.Of<IHttpCorrelationInfoAccessor>();
            var logger = NullLogger<HttpCorrelationMessageHandler>.Instance;

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new HttpCorrelationMessageHandler(accessor, options: null, logger));
        }

        [Fact]
        public void Create_WithoutLogger_Fails()
        {
            // Arrange
            var accessor = Mock.Of<IHttpCorrelationInfoAccessor>();
            var options = new HttpCorrelationClientOptionsTests();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new HttpCorrelationMessageHandler(accessor, options, logger: null));
        }
    }
}
