using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Testing.Logging;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Unit.Logging.Fixture;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class HttpClientExtensionsTests : HttpCorrelationTrackingTests
    {
        private static readonly HttpClient DefaultHttpClient = new HttpClient();

        [Fact]
        public async Task SendWithAccessor_Default_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlation);
            var logger = new InMemoryLogger();
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(statusCode, req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, HttpCorrelationProperties.UpstreamServiceHeaderName);
            });

            var client = new HttpClient(assertion);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request, accessor, logger);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task SendWithAccessor_WithCustomTransactionIdHeaderName_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlation);
            var logger = new InMemoryLogger();
            string transactionIdHeaderName = BogusGenerator.Random.AlphaNumeric(10);
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(statusCode, req =>
            {
                // Assert
                AssertHeaderValue(req, transactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, HttpCorrelationProperties.UpstreamServiceHeaderName);
            });

            var client = new HttpClient(assertion);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request, accessor, logger, options => options.TransactionIdHeaderName = transactionIdHeaderName);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task SendWithAccessor_WithCustomUpstreamServiceHeaderName_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlation);
            var logger = new InMemoryLogger();
            string upstreamServiceHeaderName = BogusGenerator.Random.AlphaNumeric(10);
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(statusCode, req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, upstreamServiceHeaderName);
            });

            var client = new HttpClient(assertion);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request, accessor, logger, options => options.UpstreamServiceHeaderName = upstreamServiceHeaderName);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task SendWithAccessor_WithCustomDependencyId_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlation);
            var logger = new InMemoryLogger();
            string dependencyId = BogusGenerator.Random.AlphaNumeric(10);
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(statusCode, req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderValue(req, HttpCorrelationProperties.UpstreamServiceHeaderName, dependencyId);
            });

            var client = new HttpClient(assertion);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request, accessor, logger, options => options.GenerateDependencyId = () => dependencyId);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, dependencyId, statusCode);
        }

        [Fact]
        public async Task SendWithCorrelation_Default_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var logger = new InMemoryLogger();
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(statusCode, req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, HttpCorrelationProperties.UpstreamServiceHeaderName);
            });

            var client = new HttpClient(assertion);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request, correlation, logger);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task SendWithCorrelation_WithCustomTransactionIdHeaderName_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var logger = new InMemoryLogger();
            string transactionIdHeaderName = BogusGenerator.Random.AlphaNumeric(10);
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(statusCode, req =>
            {
                // Assert
                AssertHeaderValue(req, transactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, HttpCorrelationProperties.UpstreamServiceHeaderName);
            });

            var client = new HttpClient(assertion);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request, correlation, logger, options => options.TransactionIdHeaderName = transactionIdHeaderName);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task SendWithCorrelation_WithCustomUpstreamServiceHeaderName_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var logger = new InMemoryLogger();
            string upstreamServiceHeaderName = BogusGenerator.Random.AlphaNumeric(10);
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(statusCode, req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderAvailable(req, upstreamServiceHeaderName);
            });

            var client = new HttpClient(assertion);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request, correlation, logger, options => options.UpstreamServiceHeaderName = upstreamServiceHeaderName);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, statusCode);
        }

        [Fact]
        public async Task SendWithCorrelation_WithCustomDependencyId_TracksRequest()
        {
            // Arrange
            CorrelationInfo correlation = GenerateCorrelationInfo();
            var logger = new InMemoryLogger();
            string dependencyId = BogusGenerator.Random.AlphaNumeric(10);
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();
            var assertion = new AssertHttpMessageHandler(statusCode, req =>
            {
                // Assert
                AssertHeaderValue(req, HttpCorrelationProperties.TransactionIdHeaderName, correlation.TransactionId);
                AssertHeaderValue(req, HttpCorrelationProperties.UpstreamServiceHeaderName, dependencyId);
            });

            var client = new HttpClient(assertion);
            HttpRequestMessage request = GenerateHttpRequestMessage();

            // Act
            await client.SendAsync(request, correlation, logger, options => options.GenerateDependencyId = () => dependencyId);

            // Assert
            string message = Assert.Single(logger.Messages);
            AssertLoggedHttpDependency(message, request, dependencyId, statusCode);
        }

        [Fact]
        public void SendWithAccessorWithoutOptions_WithoutRequest_Fails()
        {
            // Arrange
            var accessor = Mock.Of<IHttpCorrelationInfoAccessor>();
            var logger = NullLogger.Instance;

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request: null, accessor, logger));
        }

        [Fact]
        public void SendWithAccessorWithoutOptions_WithoutAccessor_Fails()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var logger = NullLogger.Instance;

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request, correlationAccessor: null, logger));
        }

        [Fact]
        public void SendWithAccessorWithoutOptions_WithoutLogger_Fails()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var accessor = Mock.Of<IHttpCorrelationInfoAccessor>();

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request, accessor, logger: null));
        }

        [Fact]
        public void SendWithAccessorWithOptions_WithoutRequest_Fails()
        {
            // Arrange
            var accessor = Mock.Of<IHttpCorrelationInfoAccessor>();
            var logger = NullLogger.Instance;

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request: null, accessor, logger, configureOptions: options => { }));
        }

        [Fact]
        public void SendWithAccessorWithOptions_WithoutAccessor_Fails()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var logger = NullLogger.Instance;

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request, correlationAccessor: null, logger, configureOptions: options => { }));
        }

        [Fact]
        public void SendWithAccessorWithOptions_WithoutLogger_Fails()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var accessor = Mock.Of<IHttpCorrelationInfoAccessor>();

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request, accessor, logger: null, configureOptions: options => { }));
        }

        [Fact]
        public void SendWithCorrelationWithoutOptions_WithoutRequest_Fails()
        {
            // Arrange
            var correlation = new CorrelationInfo("operation-id", "transaction-id");
            var logger = NullLogger.Instance;

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request: null, correlation, logger));
        }

        [Fact]
        public void SendWithCorrelationWithoutOptions_WithoutAccessor_Fails()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var logger = NullLogger.Instance;

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request, correlationInfo: null, logger));
        }

        [Fact]
        public void SendWithCorrelationWithoutOptions_WithoutLogger_Fails()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var correlation = new CorrelationInfo("operation-id", "transaction-id");

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request, correlation, logger: null));
        }

        [Fact]
        public void SendWithCorrelationWithOptions_WithoutRequest_Fails()
        {
            // Arrange
            var correlation = new CorrelationInfo("operation-id", "transaction-id");
            var logger = NullLogger.Instance;

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request: null, correlation, logger, configureOptions: options => { }));
        }

        [Fact]
        public void SendWithCorrelationWithOptions_WithoutAccessor_Fails()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var logger = NullLogger.Instance;

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request, correlationInfo: null, logger, configureOptions: options => { }));
        }

        [Fact]
        public void SendWithCorrelationWithOptions_WithoutLogger_Fails()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var correlation = new CorrelationInfo("operation-id", "transaction-id");

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(() => DefaultHttpClient.SendAsync(request, correlation, logger: null, configureOptions: options => { }));
        }
    }
}
