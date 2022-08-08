using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// Represents an <see cref="HttpCorrelationTemplate{THttpRequest,THttpResponse}"/> implementation
    /// that extracts and sets HTTP correlation throughout Azure Functions HTTP trigger applications.
    /// </summary>
    public class AzureFunctionsHttpCorrelation : HttpCorrelationTemplate<HttpRequestData, HttpResponseData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsHttpCorrelation" /> class.
        /// </summary>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="correlationInfoAccessor">The instance to set and retrieve the <see cref="CorrelationInfo"/> instance.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="correlationInfoAccessor"/> is <c>null</c>.</exception>
        public AzureFunctionsHttpCorrelation(
            HttpCorrelationInfoOptions options, 
            IHttpCorrelationInfoAccessor correlationInfoAccessor, 
            ILogger<AzureFunctionsHttpCorrelation> logger) 
            : base(options, correlationInfoAccessor, logger)
        {
        }

        /// <summary>
        /// Gets the HTTP request headers from the incoming <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The incoming HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        protected override IHeaderDictionary GetRequestHeaders(HttpRequestData request)
        {
            Guard.NotNull(request, nameof(request), "Requires a HTTP request instance to retrieve the HTTP request headers");

            Dictionary<string, StringValues> dictionary = 
                request.Headers.ToDictionary(
                p => p.Key,
                p => new StringValues(p.Value.ToArray()));
            
            return new HeaderDictionary(dictionary);
        }

        /// <summary>
        /// Set the <paramref name="headerName"/>, <paramref name="headerValue"/> combination in the outgoing <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The outgoing HTTP response that gets a HTTP correlation header.</param>
        /// <param name="headerName">The HTTP correlation response header name.</param>
        /// <param name="headerValue">The HTTP correlation response header value.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="response"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> or <paramref name="headerValue"/> is blank.</exception>
        protected override void SetHttpResponseHeader(HttpResponseData response, string headerName, string headerValue)
        {
            Guard.NotNull(response, nameof(response), "Requires a HTTP response to set the HTTP correlation headers");
            Guard.NotNullOrWhitespace(headerName, nameof(headerName), "Requires a non-blank HTTP correlation header name to set the HTTP correlation header in the HTTP request");
            Guard.NotNullOrWhitespace(headerValue, nameof(headerValue), "Requires a non-blank HTTP correlation header value to set the HTTP correlation header in the HTTP request");

            response.Headers.Add(headerName, headerValue);
        }
    }
}
