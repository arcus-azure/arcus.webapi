using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Arcus.Observability.Correlation;
using Bogus;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public abstract class HttpCorrelationTrackingTests
    {
        private static readonly Regex DependencyIdRegex = new Regex(@"ID [a-z0-9]{8}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{12}", RegexOptions.Compiled);
        protected static readonly Faker BogusGenerator = new Faker();

        protected static CorrelationInfo GenerateCorrelationInfo()
        {
            return new CorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
        }

        protected static HttpRequestMessage GenerateHttpRequestMessage()
        {
            HttpMethod method = GenerateHttpMethod();
            var uri = new Uri(BogusGenerator.Internet.UrlWithPath());
            var request = new HttpRequestMessage(method, uri);
            
            return request;
        }

        protected static HttpMethod GenerateHttpMethod()
        {
            return BogusGenerator.PickRandom(
                HttpMethod.Get,
                HttpMethod.Delete,
                HttpMethod.Head,
                HttpMethod.Options,
                HttpMethod.Patch,
                HttpMethod.Post,
                HttpMethod.Put,
                HttpMethod.Trace);
        }

        protected static void AssertHeaderValue(HttpRequestMessage request, string headerName, string headerValue)
        {
            string actual = Assert.Single(request.Headers.GetValues(headerName));
            Assert.Equal(headerValue, actual);
        }

        protected static void AssertLoggedHttpDependency(string message, HttpRequestMessage request, HttpStatusCode statusCode)
        {
            string path = request.RequestUri?.AbsolutePath;
            Assert.False(string.IsNullOrWhiteSpace(path), "HTTP request path should not be blank when asserting on the tracked HTTP dependency");

            Assert.StartsWith($"Http {request.Method} {path}", message);
            Assert.Matches(DependencyIdRegex, message);
            Assert.Contains($"ResultCode: {(int)statusCode}", message);
        }

        protected static void AssertLoggedHttpDependency(string message, HttpRequestMessage request, string dependencyId, HttpStatusCode statusCode)
        {
            string path = request.RequestUri?.AbsolutePath;
            Assert.False(string.IsNullOrWhiteSpace(path), "HTTP request path should not be blank when asserting on the tracked HTTP dependency");

            Assert.StartsWith($"Http {request.Method} {path}", message);
            Assert.Contains($"ID {dependencyId}", message);
            Assert.Contains($"ResultCode: {(int)statusCode}", message);
        }

        protected static void AssertHeaderAvailable(HttpRequestMessage request, string headerName)
        {
            string actual = Assert.Single(request.Headers.GetValues(headerName));
            Assert.False(string.IsNullOrWhiteSpace(actual), $"HTTP request message should have header with name '{headerName}'");
        }
    }
}
