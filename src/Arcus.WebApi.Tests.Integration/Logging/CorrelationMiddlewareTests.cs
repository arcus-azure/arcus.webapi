using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Testing.Logging;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Controllers;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection("Integration")]
    public class CorrelationMiddlewareTests
    {
        private const string DefaultOperationId = "RequestId", 
                             DefaultTransactionId = "X-Transaction-ID";

        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationMiddlewareTests" /> class.
        /// </summary>
        public CorrelationMiddlewareTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }
        
        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNotAllowTransactionInRequest_ResponseWithBadRequest()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.Transaction.AllowInRequest = false))
                .Configure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(CorrelationController.GetRoute)
                    .WithHeader(DefaultTransactionId, $"transaction-{Guid.NewGuid()}");
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultOperationId);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultTransactionId);
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithCorrelationInfoOptionsNotGenerateTransactionId_ResponseWithoutTransactionId()
        {
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.Transaction.GenerateWhenNotSpecified = false))
                .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(CorrelationController.GetRoute);
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Contains(response.Headers, header => header.Key == DefaultOperationId);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultTransactionId);
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNonTransactionIncludeInResponse_ResponseWithoutCorrelationHeaders()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.Transaction.IncludeInResponse = false))
                .PreConfigure(app => app.UseHttpCorrelation());
            
            await using (var service = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                var request = HttpRequestBuilder.Get(CorrelationController.GetRoute);
                using (HttpResponseMessage response = await service.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Contains(response.Headers, header => header.Key == DefaultOperationId);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultTransactionId);
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithCorrelateOptionsCustomGenerateTransactionId_ResponseWitCustomGeneratedTransactionId()
        {
            // Arrange
            var expectedTransactionId = $"transaction-{Guid.NewGuid():N}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.Transaction.GenerateId = () => expectedTransactionId))
                .PreConfigure(app => app.UseHttpCorrelation());
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(CorrelationController.GetRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Contains(response.Headers, header => header.Key == DefaultOperationId);
                
                    string actualTransactionId = GetResponseHeader(response, DefaultTransactionId);
                    Assert.Equal(expectedTransactionId, actualTransactionId);
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNonOperationIncludeInResponse_ResponseWithoutCorrelationHeaders()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.Operation.IncludeInResponse = false))
                .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(CorrelationController.GetRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultOperationId);
                    Assert.Contains(response.Headers, header => header.Key == DefaultTransactionId);
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithoutCorrelationHeaders_ResponseWithCorrelationHeadersAndCorrelationAccess()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation())
                .PreConfigure(app => app.UseHttpCorrelation());
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(CorrelationController.GetRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                
                    string correlationId = GetResponseHeader(response, DefaultTransactionId);
                    string requestId = GetResponseHeader(response, DefaultOperationId);

                    string json = await response.Content.ReadAsStringAsync();
                    var content = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "" });
                    Assert.False(String.IsNullOrWhiteSpace(content.TransactionId), "Accessed 'X-Transaction-ID' cannot be blank");
                    Assert.False(String.IsNullOrWhiteSpace(content.OperationId), "Accessed 'X-Operation-ID' cannot be blank");
                
                    Assert.Equal(correlationId, content.TransactionId);
                    Assert.Equal(requestId, content.OperationId);
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithCorrelationHeader_ResponseWithSameCorrelationHeader()
        {
            // Arrange
            string expected = $"transaction-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation())
                .PreConfigure(app => app.UseHttpCorrelation());
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(CorrelationController.GetRoute)
                    .WithHeader(DefaultTransactionId, expected);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string actual = GetResponseHeader(response, DefaultTransactionId);
                    Assert.Equal(expected, actual);
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithRequestIdHeader_ResponseWithDifferentRequestIdHeader()
        {
            // Arrange
            string expected = $"operation-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation())
                .PreConfigure(app => app.UseHttpCorrelation());
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(CorrelationController.GetRoute)
                    .WithHeader(DefaultOperationId, expected);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string actual = GetResponseHeader(response, DefaultOperationId);
                    Assert.NotEqual(expected, actual);
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithCorrelateOptionsCustomGenerateOperationId_ResponseWitCustomGeneratedOperationId()
        {
            // Arrange
            var expectedOperationId = $"operation-{Guid.NewGuid():N}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.Operation.GenerateId = () => expectedOperationId))
                .PreConfigure(app => app.UseTraceIdentifier(opt => opt.EnableTraceIdentifier = false)
                                        .UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(CorrelationController.GetRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Contains(response.Headers, header => header.Key == DefaultTransactionId);

                    string actualOperationId = GetResponseHeader(response, DefaultOperationId);
                    Assert.Equal(expectedOperationId, actualOperationId);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendRequest_WithCorrelateOptionsUpstreamServiceExtractFromRequest_UsesUpstreamOperationId(bool extractFromRequest)
        {
            // Arrange
            var operationParentId = Guid.NewGuid().ToString();
            var requestId = $"|{Guid.NewGuid()}.{operationParentId}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.UpstreamService.ExtractFromRequest = extractFromRequest))
                .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(CorrelationController.GetRoute)
                    .WithHeader("Request-Id", requestId);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string json = await response.Content.ReadAsStringAsync();
                    string actualParentId = JObject.Parse(json).GetValue("OperationParentId").Value<string>();
                    Assert.Equal(extractFromRequest, operationParentId == actualParentId);
                }
            }
        }

        [Theory]
        [InlineData(false, "|abc.def", null)]
        [InlineData(true, "|abc.def", "def")]
        [InlineData(false, "abc", null)]
        [InlineData(true, "abc", "abc")]
        public async Task SendRequest_WithCorrelateOptionsUpstreamServiceExtractFromRequest_UsesRequestId(bool extractFromRequest, string requestId, string expectedParentId)
        {
            // Arrange
            var options = new TestApiServerOptions()
                          .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.UpstreamService.ExtractFromRequest = extractFromRequest))
                          .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                              .Get(CorrelationController.GetRoute)
                              .WithHeader("Request-Id", requestId);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string json = await response.Content.ReadAsStringAsync();
                    string actualParentId = JObject.Parse(json).GetValue("OperationParentId").Value<string>();
                    Assert.Equal(extractFromRequest, expectedParentId == actualParentId);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendRequest_WithCorrelateOptionsOperationParentExtractFromRequest_UsesUpstreamOperationId(bool extractFromRequest)
        {
            // Arrange
            var operationParentId = Guid.NewGuid().ToString();
            var requestId = $"|{Guid.NewGuid()}.{operationParentId}";
            var options = new TestApiServerOptions()
                          .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.OperationParent.ExtractFromRequest = extractFromRequest))
                          .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                              .Get(CorrelationController.GetRoute)
                              .WithHeader("Request-Id", requestId);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string json = await response.Content.ReadAsStringAsync();
                    string actualParentId = JObject.Parse(json).GetValue("OperationParentId").Value<string>();
                    Assert.Equal(extractFromRequest, operationParentId == actualParentId);
                }
            }
        }

        [Theory]
        [InlineData(false, "|abc.def", null)]
        [InlineData(true, "|abc.def", "def")]
        [InlineData(false, "abc", null)]
        [InlineData(true, "abc", "abc")]
        public async Task SendRequest_WithCorrelateOptionsOperationParentExtractFromRequest_UsesRequestId(bool extractFromRequest, string requestId, string expectedParentId)
        {
            // Arrange
            var options = new TestApiServerOptions()
                          .ConfigureServices(services => services.AddHttpCorrelation(opt => opt.OperationParent.ExtractFromRequest = extractFromRequest))
                          .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                              .Get(CorrelationController.GetRoute)
                              .WithHeader("Request-Id", requestId);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string json = await response.Content.ReadAsStringAsync();
                    string actualParentId = JObject.Parse(json).GetValue("OperationParentId").Value<string>();
                    Assert.Equal(extractFromRequest, expectedParentId == actualParentId);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendRequest_WithCorrelateOptionsOperationParentCustomOperationParentIHeaderName_UsesUpstreamOperationId(bool extractFromRequest)
        {
            // Arrange
            var operationParentId = Guid.NewGuid().ToString();
            var requestId = $"|{Guid.NewGuid()}.{operationParentId}";
            var operationParentIdHeaderName = "My-Request-Id";
            var options = new TestApiServerOptions()
                          .ConfigureServices(services => services.AddHttpCorrelation(opt =>
                          {
                              opt.OperationParent.ExtractFromRequest = extractFromRequest;
                              opt.OperationParent.OperationParentIdHeaderName = operationParentIdHeaderName;
                          }))
                          .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                              .Get(CorrelationController.GetRoute)
                              .WithHeader(operationParentIdHeaderName, requestId);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string json = await response.Content.ReadAsStringAsync();
                    string actualParentId = JObject.Parse(json).GetValue("OperationParentId").Value<string>();
                    Assert.Equal(extractFromRequest, operationParentId == actualParentId);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendRequest_WithCorrelateOptionsUpstreamServiceCustomOperationParentIHeaderName_UsesUpstreamOperationId(bool extractFromRequest)
        {
            // Arrange
            var operationParentId = Guid.NewGuid().ToString();
            var requestId = $"|{Guid.NewGuid()}.{operationParentId}";
            var operationParentIdHeaderName = "My-Request-Id";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation(opt =>
                {
                    opt.UpstreamService.ExtractFromRequest = extractFromRequest;
                    opt.UpstreamService.OperationParentIdHeaderName = operationParentIdHeaderName;
                }))
                .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(CorrelationController.GetRoute)
                    .WithHeader(operationParentIdHeaderName, requestId);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string json = await response.Content.ReadAsStringAsync();
                    string actualParentId = JObject.Parse(json).GetValue("OperationParentId").Value<string>();
                    Assert.Equal(extractFromRequest, operationParentId == actualParentId);
                }
            }
        }

        [Fact]
        public async Task SendRequestWithCorrelateInfo_SetsCorrelationInfo_ResponseWithUpdatedCorrelationInfo()
        {
            // Arrange
            var correlationInfo = new CorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}");
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation())
                .PreConfigure(app => app.UseHttpCorrelation());
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Post(CorrelationController.SetCorrelationRoute)
                    .WithHeader(DefaultOperationId, correlationInfo.OperationId)
                    .WithHeader(DefaultTransactionId, correlationInfo.TransactionId);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string actualOperationId = GetResponseHeader(response, DefaultOperationId);
                    string actualTransactionId = GetResponseHeader(response, DefaultTransactionId);

                    Assert.Equal(correlationInfo.OperationId, actualOperationId);
                    Assert.Equal(correlationInfo.TransactionId, actualTransactionId);
                }
            }
        }

        [Fact]
        public async Task SendRequestWithAlternateCorrelationAccessor_RemovesCorrelation_ResponseWithoutCorrelationInfo()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation()
                                                       .AddScoped<IHttpCorrelationInfoAccessor, NullCorrelationInfoAccessor>())
                .PreConfigure(app => app.UseHttpCorrelation());

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(EchoController.GetPostRoute)
                    .WithHeader(DefaultOperationId, $"operation-{Guid.NewGuid()}")
                    .WithHeader(DefaultTransactionId, $"transaction-{Guid.NewGuid()}");
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultOperationId);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultTransactionId);
                }
            }
        }
        
        private static string GetResponseHeader(HttpResponseMessage response, string headerName)
        {
            (string key, IEnumerable<string> values) = Assert.Single(response.Headers, header => header.Key == headerName);
            
            Assert.NotNull(values);
            string value = Assert.Single(values);
            Assert.False(String.IsNullOrWhiteSpace(value), $"Response header '{headerName}' cannot be blank");

            return value;
        }
    }
}
