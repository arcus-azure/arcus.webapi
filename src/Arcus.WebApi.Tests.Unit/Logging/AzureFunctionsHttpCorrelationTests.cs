using Arcus.Observability.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Unit.Logging.Fixture;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Arcus.WebApi.Logging.Core.Correlation.HttpCorrelationProperties;
using Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class AzureFunctionsAzureFunctionsHttpCorrelationTests
    {
        [Fact]
        public async Task HttpCorrelationMiddleware_WithValidInput_ResponseWithNextResponse()
        {
            // Arrange
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            var context = TestFunctionContext.Create(
                configureServices: services => services.AddSingleton(correlation));

            var middleware = new AzureFunctionsCorrelationMiddleware();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                HttpRequestData request = await ctx.GetHttpRequestDataAsync();
                ctx.GetInvocationResult().Value = request.CreateResponse(HttpStatusCode.Accepted);
            });

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.NotNull(correlationInfo);
        }

        [Fact]
        public async Task HttpCorrelationMiddleware_WithDisallowedTransactionId_ResponseWithBadRequest()
        {
            // Arrange
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions()
            {
                Transaction = { AllowInRequest = false }
            });

            var context = TestFunctionContext.Create(
                request => request.Headers.Add(TransactionIdHeaderName, "some disallowed transaction ID"),
                services => services.AddSingleton(correlation));

            var middleware = new AzureFunctionsCorrelationMiddleware();
            
            // Act
            await middleware.Invoke(context, ctx => Task.CompletedTask);

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("|")]
        [InlineData("|other-id.")]
        public void TryCorrelate_WithCorrectOperationParentId_SetsOperationIds(string prefix)
        {
            // Arrange
            var operationParentId = $"operation-{Guid.NewGuid()}";
            string requestId = prefix + operationParentId;
            HttpRequestData request = CreateHttpRequest(new[]
            {
                new KeyValuePair<string, string>(UpstreamServiceHeaderName, requestId)
            });

            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(requestId, result.RequestId);

            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.NotNull(correlationInfo);
            Assert.Equal(operationParentId, correlationInfo.OperationParentId);
        }

        [Fact]
        public void TryCorrelate_WithCorrectOperationParentIdWithDisabledUpstreamExtraction_DoesntSetExpectedOperationId()
        {
            // Arrange
            var operationIdFromGeneration = $"operation-{Guid.NewGuid()}";
            var operationIdFromUpstream = $"operation-{Guid.NewGuid()}";

            HttpRequestData request = CreateHttpRequest(new[]
            {
                new KeyValuePair<string, string>(UpstreamServiceHeaderName, $"|{Guid.NewGuid()}.{operationIdFromUpstream}")
            });

            var options = new HttpCorrelationInfoOptions
            {
                UpstreamService =
                {
                    ExtractFromRequest = false,
                    GenerateId = () => operationIdFromGeneration
                }
            };
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, options);
            
            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);
            
            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Null(result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.NotEqual(operationIdFromUpstream, correlationInfo.OperationParentId);
            Assert.Equal(operationIdFromGeneration, correlationInfo.OperationParentId);
        }

        [Theory]
        [InlineData("||")]
        [InlineData("|..")]
        [InlineData(".|")]
        [InlineData("|.other-id..")]
        public void TryCorrelate_WithIncorrectOperationParentId_DoesntSetExpectedOperationId(string prefix)
        {
            // Arrange
            var operationParentId = $"operation-{Guid.NewGuid()}";
            HttpRequestData request = CreateHttpRequest(new[]
            {
                new KeyValuePair<string, string>(UpstreamServiceHeaderName, prefix + operationParentId)
            });

            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Null(result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.NotEqual(operationParentId, correlationInfo.OperationParentId);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void TryCorrelate_WithBlankOperationParentId_DoesntSetOperationId(string operationParentId)
        {
            // Arrange
            HttpRequestData request = CreateHttpRequest(new []
            {
                new KeyValuePair<string, string>(UpstreamServiceHeaderName, operationParentId)
            });

            var accessor = new StubHttpCorrelationInfoAccessor();
            var correlation = CreateHttpCorrelationForHierarchical(accessor);

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Null(result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.NotNull(correlationInfo.OperationId);
            Assert.Null(correlationInfo.OperationParentId);
        }

        [Fact]
        public void TryCorrelate_WithCustomTraceIdentifier_UsesTraceIdentifierAsOperationId()
        {
            // Arrange
            string traceIdentifier = $"invocation-{Guid.NewGuid()}";
            HttpRequestData request = CreateHttpRequest();
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.Equal(traceIdentifier, correlationInfo.OperationId);
        }

        [Fact]
        public void TryCorrelate_WithCustomTransactionIdHeader_FailsToCorrelate()
        {
            // Arrange
            string headerName = "My-Transaction-Id";
            string transactionId = $"transaction-{Guid.NewGuid()}";
            HttpRequestData request = CreateHttpRequest(new []
            {
                new KeyValuePair<string, string>(headerName, transactionId)
            });
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Transaction = { HeaderName = headerName }
            });

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.Equal(transactionId, correlationInfo.TransactionId);
        }

        [Fact]
        public void TryCorrelate_WithDisallowedTransactionId_FailsToCorrelate()
        {
            // Arrange
            HttpRequestData request = CreateHttpRequest(new []
            {
                new KeyValuePair<string, string>(TransactionIdHeaderName, $"transaction-{Guid.NewGuid()}")
            });
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Transaction = { AllowInRequest = false }
            });

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void TryCorrelate_WithNotAllowedTransactionId_SucceedsToCorrelate()
        {
            // Arrange
            HttpRequestData request = CreateHttpRequest();
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Transaction = { AllowInRequest = false }
            });

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.NotNull(correlationInfo.TransactionId);
        }

        [Fact]
        public void TryCorrelate_WithAlreadyPresentTransactionId_UsesTransactionId()
        {
            // Arrange
            string transactionId = $"transaction-{Guid.NewGuid()}";
            HttpRequestData request = CreateHttpRequest(new []
            {
                new KeyValuePair<string, string>(TransactionIdHeaderName, transactionId)
            });
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.Equal(transactionId, correlationInfo.TransactionId);
        }

        [Fact]
        public void TryCorrelate_WithCustomTransactionIdGeneration_UsesTransactionId()
        {
            // Arrange
            string transactionId = $"transaction-{Guid.NewGuid()}";
            HttpRequestData request = CreateHttpRequest();
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Transaction = { GenerateId = () => transactionId }
            });

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.Equal(transactionId, correlationInfo.TransactionId);
        }

        [Fact]
        public void TryCorrelate_WithCustomOperationIdGeneration_UsesOperationId()
        {
            // Arrange
            string operationId = $"operation-{Guid.NewGuid()}";
            HttpRequestData request = CreateHttpRequest();
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Operation = { GenerateId = () => operationId }
            });

            // Act
            HttpCorrelationResult result = correlation.TrySettingCorrelationFromRequest(request, traceIdentifier: null);

            // Assert
            Assert.True(result.IsSuccess, result.ErrorMessage);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.Equal(operationId, correlationInfo.OperationId);
        }

        [Fact]
        public void SetCorrelationToResponse_WithValidHttpCorrelationResult_Succeeds()
        {
            // Arrange
            string operationParentId = $"parent-{Guid.NewGuid()}";
            string requestId = $"|{operationParentId}";
            var result = HttpCorrelationResult.Success(requestId);

            var correlationInfo = new CorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}", operationParentId);
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            HttpResponseData response = CreateHttpResponse();

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertResponseHeader(response, OperationIdHeaderName, correlationInfo.OperationId);
            AssertResponseHeader(response, TransactionIdHeaderName, correlationInfo.TransactionId);
            AssertResponseHeader(response, UpstreamServiceHeaderName, requestId);
        }

        [Fact]
        public void SetCorrelationToResponse_WithoutRequestId_IsNotPresentInResponse()
        {
            // Arrange
            CorrelationInfo correlationInfo = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            HttpResponseData response = CreateHttpResponse();
            var result = HttpCorrelationResult.Success(requestId: null);

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertResponseHeader(response, OperationIdHeaderName, correlationInfo.OperationId);
            AssertResponseHeader(response, TransactionIdHeaderName, correlationInfo.TransactionId);
            AssertMissingResponseHeader(response, UpstreamServiceHeaderName);
        }

        private static CorrelationInfo GenerateCorrelationInfo()
        {
            return new CorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}", $"parent-{Guid.NewGuid()}");
        }

        [Fact]
        public void SetCorrelationToResponse_WithoutTransactionId_IsNotPresentInResponse()
        {
            // Arrange
            CorrelationInfo correlationInfo = new CorrelationInfo($"operation-{Guid.NewGuid()}", transactionId: null, $"parent-{Guid.NewGuid()}");
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            HttpResponseData response = CreateHttpResponse();
            var result = HttpCorrelationResult.Success(requestId: $"|{correlationInfo.OperationParentId}");

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertResponseHeader(response, OperationIdHeaderName, correlationInfo.OperationId);
            AssertMissingResponseHeader(response, TransactionIdHeaderName);
            AssertResponseHeader(response, UpstreamServiceHeaderName, result.RequestId);
        }

        [Fact]
        public void SetCorrelationToResponse_WithoutOperationId_IsNotPresentInResponse()
        {
            // Arrange
            var accessor = new StubHttpCorrelationInfoAccessor();
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor);

            HttpResponseData response = CreateHttpResponse();
            var result = HttpCorrelationResult.Success(requestId: null);

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertMissingResponseHeader(response, OperationIdHeaderName);
            AssertMissingResponseHeader(response, TransactionIdHeaderName);
            AssertMissingResponseHeader(response, UpstreamServiceHeaderName);
        }

        [Fact]
        public void SetCorrelationToResponse_WithoutIncludingOperationId_IsNotPresentInResponse()
        {
            // Arrange
            var correlationInfo = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            var correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Operation = { IncludeInResponse = false }
            });

            HttpResponseData response = CreateHttpResponse();
            var requestId = $"|{correlationInfo.OperationParentId}";
            var result = HttpCorrelationResult.Success(requestId);

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertMissingResponseHeader(response, OperationIdHeaderName);
            AssertResponseHeader(response, TransactionIdHeaderName, correlationInfo.TransactionId);
            AssertResponseHeader(response, UpstreamServiceHeaderName, requestId);
        }

        [Fact]
        public void SetCorrelationToResponse_WithCustomOperationIdHeaderName_IsPresentInResponse()
        {
            // Arrange
            var headerName = "My-Operation-Id";
            CorrelationInfo correlationInfo = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Operation = { HeaderName = headerName }
            });

            HttpResponseData response = CreateHttpResponse();
            var requestId = $"|{correlationInfo.OperationParentId}";
            var result = HttpCorrelationResult.Success(requestId);

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertResponseHeader(response, headerName, correlationInfo.OperationId);
            AssertResponseHeader(response, TransactionIdHeaderName, correlationInfo.TransactionId);
            AssertResponseHeader(response, UpstreamServiceHeaderName, requestId);
        }

        [Fact]
        public void SetCorrelationToResponse_WithoutIncludingTransactionId_IsNotPresentInResponse()
        {
            // Arrange
            var correlationInfo = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            var correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Transaction = { IncludeInResponse = false }
            });

            HttpResponseData response = CreateHttpResponse();
            var requestId = $"|{correlationInfo.OperationParentId}";
            var result = HttpCorrelationResult.Success(requestId);

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertResponseHeader(response, OperationIdHeaderName, correlationInfo.OperationId);
            AssertMissingResponseHeader(response, TransactionIdHeaderName);
            AssertResponseHeader(response, UpstreamServiceHeaderName, requestId);
        }

        [Fact]
        public void SetCorrelationToResponse_WithCustomTransactionIdHeaderName_IsPresentInResponse()
        {
            // Arrange
            var headerName = "My-Transaction-Id";
            CorrelationInfo correlationInfo = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                Transaction = { HeaderName = headerName }
            });

            HttpResponseData response = CreateHttpResponse();
            var requestId = $"|{correlationInfo.OperationParentId}";
            var result = HttpCorrelationResult.Success(requestId);

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertResponseHeader(response, OperationIdHeaderName, correlationInfo.OperationId);
            AssertResponseHeader(response, headerName, correlationInfo.TransactionId);
            AssertResponseHeader(response, UpstreamServiceHeaderName, requestId);
        }

        [Fact]
        public void SetCorrelationToResponse_WithoutIncludingOperationParentId_IsNotPresentInResponse()
        {
            // Arrange
            var correlationInfo = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            var correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                UpstreamService = { IncludeInResponse = false }
            });

            HttpResponseData response = CreateHttpResponse();
            var requestId = $"|{correlationInfo.OperationParentId}";
            var result = HttpCorrelationResult.Success(requestId);

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertResponseHeader(response, OperationIdHeaderName, correlationInfo.OperationId);
            AssertResponseHeader(response, TransactionIdHeaderName, correlationInfo.TransactionId);
            AssertMissingResponseHeader(response, UpstreamServiceHeaderName);
        }

        [Fact]
        public void SetCorrelationToResponse_WithCustomUpstreamServiceHeaderName_IsPresentInResponse()
        {
            // Arrange
            var headerName = "My-Request-Id";
            CorrelationInfo correlationInfo = GenerateCorrelationInfo();
            var accessor = new StubHttpCorrelationInfoAccessor(correlationInfo);
            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForHierarchical(accessor, new HttpCorrelationInfoOptions
            {
                UpstreamService = { HeaderName = headerName }
            });

            HttpResponseData response = CreateHttpResponse();
            var requestId = $"|{correlationInfo.OperationParentId}";
            var result = HttpCorrelationResult.Success(requestId);

            // Act
            correlation.SetCorrelationHeadersInResponse(response, result);

            // Assert
            AssertResponseHeader(response, OperationIdHeaderName, correlationInfo.OperationId);
            AssertResponseHeader(response, TransactionIdHeaderName, correlationInfo.TransactionId);
            AssertResponseHeader(response, headerName, requestId);
        }

        private static AzureFunctionsHttpCorrelation CreateHttpCorrelationForHierarchical(
            StubHttpCorrelationInfoAccessor accessor,
            HttpCorrelationInfoOptions options = null)
        {
            options = options ?? new HttpCorrelationInfoOptions();
            options.Format = HttpCorrelationFormat.Hierarchical;

            var correlation = new AzureFunctionsHttpCorrelation(options, accessor, NullLogger<AzureFunctionsHttpCorrelation>.Instance);
            return correlation;
        }

        private static HttpRequestData CreateHttpRequest(IEnumerable<KeyValuePair<string, string>> headers = null, FunctionContext context = null)
        {
            var request = new Mock<HttpRequestData>(context ?? CreateFunctionContext());
            request.Setup(r => r.Headers).Returns(new HttpHeadersCollection(headers ?? Enumerable.Empty<KeyValuePair<string, string>>()));

            return request.Object;
        }

        private static HttpResponseData CreateHttpResponse(FunctionContext context = null)
        {
            var response = new Mock<HttpResponseData>(context ?? CreateFunctionContext());
            response.Setup(r => r.Headers).Returns(new HttpHeadersCollection());

            return response.Object;
        }

        private static FunctionContext CreateFunctionContext(string invocationId = null)
        {
            var context = new Mock<FunctionContext>();
            context.Setup(c => c.InvocationId).Returns(invocationId);

            return context.Object;
        }

        private static void AssertResponseHeader(HttpResponseData response, string headerName, string headerValue)
        {
            Assert.True(response.Headers.TryGetValues(headerName, out IEnumerable<string> headerValues), $"HTTP response should contain '{headerName}' header");
            Assert.Equal(headerValue, Assert.Single(headerValues));
        }

        private static void AssertMissingResponseHeader(HttpResponseData response, string headerName)
        {
            Assert.False(response.Headers.Contains(headerName), $"HTTP response should not contain '{headerName}' header");
        }

        [Fact]
        public void Create_WithoutOptions_Fails()
        {
            // Arrange
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();
            ILogger<AzureFunctionsHttpCorrelation> logger = NullLogger<AzureFunctionsHttpCorrelation>.Instance;
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new AzureFunctionsHttpCorrelation(options: null, correlationInfoAccessor: correlationAccessor, logger));
        }

        [Fact]
        public void Create_WithoutCorrelationInfoAccessor_Fails()
        {
            // Arrange
            var options = new HttpCorrelationInfoOptions();
            ILogger<AzureFunctionsHttpCorrelation> logger = NullLogger<AzureFunctionsHttpCorrelation>.Instance;
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new AzureFunctionsHttpCorrelation(options, correlationInfoAccessor: null, logger: logger));
        }
    }
}
