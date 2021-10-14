using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Request tracing middleware component to log every incoming HTTP request.
    /// </summary>
    public class RequestTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTrackingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingMiddleware"/> class.
        /// </summary>
        /// <param name="options">The options to control the behavior of the request tracking.</param>
        /// <param name="next">The next pipeline function to process the HTTP context.</param>
        /// <param name="logger">The logger to write telemetry tracking during the request tracking.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/>, <paramref name="next"/>, <paramref name="logger"/> is <c>null</c>.</exception>
        public RequestTrackingMiddleware(
            RequestTrackingOptions options,
            RequestDelegate next,
            ILogger<RequestTrackingMiddleware> logger)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of options to control the behavior of the HTTP tracking middleware");
            Guard.NotNull(next, nameof(next), "Requires a function pipeline to delegate the remainder of the request processing");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write telemetry tracking during the request processing");

            _next = next;
            _logger = logger;
            Options = options;
        }

        /// <summary>
        /// Gets the consumer-configured options to control the behavior of the request tracking.
        /// </summary>
        public RequestTrackingOptions Options { get; }
        
        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="httpContext">The <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> for the current request.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the execution of this middleware.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="httpContext"/> is <c>null</c>.</exception>
        public async Task Invoke(HttpContext httpContext)
        {
            Guard.NotNull(httpContext, nameof(httpContext), "Requires a HTTP context instance to track the incoming request and outgoing response");
            Guard.NotNull(httpContext.Request, nameof(httpContext), "Requires a HTTP request in the context to track the request");
            Guard.NotNull(httpContext.Response, nameof(httpContext), "Requires a HTTP response in the context to track the request");

            if (IsRequestPathOmitted(httpContext.Request))
            {
                await _next(httpContext);
            }
            else
            {
                var endpoint = httpContext.Features.Get<IEndpointFeature>();
                if (endpoint?.Endpoint?.Metadata is null)
                {
                    _logger.LogTrace("Cannot determine whether or not the endpoint contains the '{Attribute}' because the endpoint tracking (`IApplicationBuilder.UseRouting()` or `.UseEndpointRouting()`) was not activated before the request tracking middleware; or the route was not found", nameof(ExcludeRequestTrackingAttribute));
                    await TrackRequest(httpContext, Exclude.None, Array.Empty<StatusCodeRange>());
                }
                else if (endpoint.Endpoint.Metadata.GetMetadata<ExcludeRequestTrackingAttribute>() != null)
                {
                    _logger.LogTrace("Skip request tracking for endpoint '{Endpoint}' due to the '{Attribute}' attribute on the endpoint", endpoint.Endpoint.DisplayName, nameof(ExcludeRequestTrackingAttribute));
                    await _next(httpContext);
                }
                else
                {
                    RequestTrackingAttribute[] attributes = DetermineAppliedAttributes(endpoint);
                    Exclude filter = DetermineExclusionFilter(attributes);
                    StatusCodeRange[] trackedStatusCodeRanges = DetermineTrackedStatusCodeRanges(attributes);
                
                    await TrackRequest(httpContext, filter, trackedStatusCodeRanges);
                }
            }
        }

        private bool IsRequestPathOmitted(HttpRequest request)
        {
            IEnumerable<string> allOmittedRoutes = Options.OmittedRoutes ?? new Collection<string>();
            string[] matchedOmittedRoutes = 
                allOmittedRoutes
                    .Select(omittedRoute => omittedRoute?.StartsWith("/") == true ? omittedRoute : "/" + omittedRoute)
                    .Where(omittedRoute => request.Path.StartsWithSegments(omittedRoute, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

            if (matchedOmittedRoutes.Length > 0)
            {
                string endpoint = request.GetDisplayUrl();
                string formattedOmittedRoutes = String.Join(", ", matchedOmittedRoutes);
                _logger.LogTrace("Skip request tracking for endpoint '{Endpoint}' due to an omitted route(s) '{OmittedRoutes}' specified in the options", endpoint, formattedOmittedRoutes);
                
                return true;
            }
            
            return false;
        }

        private RequestTrackingAttribute[] DetermineAppliedAttributes(IEndpointFeature endpoint)
        {
            if (endpoint?.Endpoint?.Metadata is null)
            {
                _logger.LogTrace("Cannot determine whether or not the endpoint contains the '{OptionsAttribute}' because the endpoint tracking (`IApplicationBuilder.UseRouting()` or `.UseEndpointRouting()`) was not activated before the request tracking middleware; or the route was not found", nameof(RequestTrackingAttribute));
                return Array.Empty<RequestTrackingAttribute>();
            }
            
            RequestTrackingAttribute[] attributes = endpoint.Endpoint.Metadata.OfType<RequestTrackingAttribute>().ToArray();
            return attributes;
        }

        private Exclude DetermineExclusionFilter(RequestTrackingAttribute[] attributes)
        {
            if (attributes.Length <= 0)
            {
                _logger.LogTrace("No '{Attribute}' found on endpoint, continue with request tracking including both request and response bodies", nameof(ExcludeRequestTrackingAttribute));
                return Exclude.None;
            }
            
            Exclude filter = attributes.Aggregate(Exclude.None, (acc, item) => acc | item.Filter);
            return filter;
        }

        private StatusCodeRange[] DetermineTrackedStatusCodeRanges(RequestTrackingAttribute[] attributes)
        {
            if (attributes.Length <= 0)
            {
                _logger.LogTrace("No '{Attribute}' found on endpoint, continue with request tracking including all HTTP status codes", nameof(ExcludeRequestTrackingAttribute));
                return Array.Empty<StatusCodeRange>();
            }
            
            StatusCodeRange[] statusCodes = 
                attributes.Where(attribute => attribute.StatusCodeRange != null)
                          .Select(attribute => attribute.StatusCodeRange)
                          .ToArray();
            
            return statusCodes;
        }

        private async Task TrackRequest(HttpContext httpContext, Exclude attributeExcludeFilter, StatusCodeRange[] attributeTrackedStatusCodes)
        {
            var stopwatch = Stopwatch.StartNew();
            bool includeRequestBody = ShouldIncludeRequestBody(attributeExcludeFilter);
            bool includeResponseBody = ShouldIncludeResponseBody(attributeExcludeFilter);
            
            string requestBody = await GetPotentialRequestBodyAsync(httpContext, includeRequestBody);

            // Response body doesn't support (built-in) buffering and is not seekable, so we're storing temporary the response stream in our own seekable stream,
            // which we later (*) replace back with the original response stream.
            // If we don't store it in our own seekable stream first, we would read the response stream for tracking and could not use the same stream to respond to the request.
            Stream originalResponseBodyStream = null;
            using (Stream temporaryResponseBodyStream = DetermineResponseBodyBuffer(includeResponseBody))
            {
                if (includeResponseBody)
                {
                    originalResponseBodyStream = httpContext.Response.Body;
                    httpContext.Response.Body = temporaryResponseBodyStream;
                }

                try
                {
                    await _next(httpContext);
                }
                finally
                {
                    if (AllowedToTrackStatusCode(httpContext, attributeTrackedStatusCodes))
                    {
                        string responseBody = await GetPotentialResponseBodyAsync(httpContext, includeResponseBody);

                        stopwatch.Stop();
                        LogRequest(requestBody, responseBody, httpContext, stopwatch.Elapsed); 
                    }

                    if (includeResponseBody)
                    {
                        // (*) Copy back the seekable/temporary response body stream to the original response body stream,
                        // for the remaining middleware components that comes after this one.
                        await CopyTemporaryStreamToResponseStreamAsync(temporaryResponseBodyStream, originalResponseBodyStream);
                    }
                }
            }
        }

        private bool ShouldIncludeRequestBody(Exclude attributeExcludeFilter)
        {
            bool includeRequestBody = Options.IncludeRequestBody && attributeExcludeFilter.HasFlag(Exclude.RequestBody) == false;
            if (includeRequestBody)
            {
                _logger.LogTrace("Request tracking will include the request's body as the options '{OptionName}' = '{OptionValue}' and the '{Attribute}' doesn't exclude the request body", nameof(Options.IncludeRequestBody), Options.IncludeRequestBody, nameof(RequestTrackingAttribute));   
            }
            
            return includeRequestBody;
        }

        private bool ShouldIncludeResponseBody(Exclude attributeExcludeFilter)
        {
            bool includeResponseBody = Options.IncludeResponseBody && attributeExcludeFilter.HasFlag(Exclude.ResponseBody) == false;
            if (includeResponseBody)
            {
                _logger.LogTrace("Request tracking will include the response's body as the options '{OptionName}' = '{OptionValue}' and the '{Attribute}' doesn't exclude the response body", nameof(Options.IncludeResponseBody), Options.IncludeResponseBody, nameof(RequestTrackingAttribute));   
            }

            return includeResponseBody;
        }

        private static Stream DetermineResponseBodyBuffer(bool includeResponseBody)
        {
            if (includeResponseBody)
            {
                var responseBodyBuffer = new MemoryStream();
                return responseBodyBuffer;
            }

            return Stream.Null;
        }

        private void LogRequest(string requestBody, string responseBody, HttpContext httpContext, TimeSpan duration)
        {
            try
            {
                IDictionary<string, StringValues> telemetryContext = GetRequestHeaders(httpContext);

                if (string.IsNullOrWhiteSpace(requestBody) == false)
                {
                    telemetryContext.Add("Body", "Request body is now available in 'RequestBody' dimension");
                    telemetryContext.Add("RequestBody", requestBody);
                }

                if (string.IsNullOrWhiteSpace(responseBody) == false)
                {
                    telemetryContext.Add("ResponseBody", responseBody);
                }

                Dictionary<string, object> logContext = telemetryContext.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
                _logger.LogRequest(httpContext.Request, httpContext.Response, duration, logContext);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failed to track request due to an unexpected failure");
            }
        }

        private IDictionary<string, StringValues> GetRequestHeaders(HttpContext httpContext)
        {
            if (Options.IncludeRequestHeaders)
            {
                _logger.LogTrace("Prepare for the request headers to be tracked...");
                IDictionary<string, StringValues> sanitizedHeaders = SanitizeRequestHeaders(httpContext.Request.Headers);
                if (sanitizedHeaders != null && sanitizedHeaders.Count > 0)
                {
                    string headerNames = String.Join(", ", sanitizedHeaders.Keys.Select(k => $"'{k}'"));
                    _logger.LogTrace("Found {RequestHeaders} request headers to be tracked", headerNames);

                    return sanitizedHeaders;
                }

                _logger.LogWarning("No request headers were found to be tracked");
            }

            return new Dictionary<string, StringValues>();
        }

        /// <summary>
        /// Extracts information from the given HTTP <paramref name="requestHeaders"/> to include in the request tracking context.
        /// </summary>
        /// <param name="requestHeaders">The headers of the current HTTP request.</param>
        protected virtual IDictionary<string, StringValues> SanitizeRequestHeaders(IDictionary<string, StringValues> requestHeaders)
        {
            return requestHeaders.Where(header => Options.OmittedHeaderNames?.Contains(header.Key) == false);
        }

        private async Task<string> GetPotentialRequestBodyAsync(HttpContext httpContext, bool includeRequestBody)
        {
            if (includeRequestBody)
            {
                httpContext.Request.EnableBuffering();
                
                string requestBody = await GetBodyAsync(httpContext.Request.Body, Options.RequestBodyBufferSize, "Request");
                string sanitizedBody = SanitizeRequestBody(httpContext.Request, requestBody);
                
                return sanitizedBody;
            }

            return null;
        }

        /// <summary>
        /// Extracts information from the HTTP <paramref name="requestBody"/> to include in the request tracking context.
        /// </summary>
        /// <param name="request">The current HTTP request.</param>
        /// <param name="requestBody">The body of the current HTTP request.</param>
        protected virtual string SanitizeRequestBody(HttpRequest request, string requestBody)
        {
            return requestBody;
        }

        private async Task<string> GetPotentialResponseBodyAsync(HttpContext httpContext, bool includeResponseBody)
        {
            if (includeResponseBody)
            {
                string responseBody = await GetBodyAsync(httpContext.Response.Body, Options.ResponseBodyBufferSize, "Response");
                string sanitizedBody = SanitizeResponseBody(httpContext.Response, responseBody);
                
                return sanitizedBody;
            }
            
            return null;
        }

        /// <summary>
        /// Extracts information from the HTTP <paramref name="responseBody"/> to include in the request tracking context.
        /// </summary>
        /// <param name="response">The current HTTP response.</param>
        /// <param name="responseBody">The body of the current HTTP response.</param>
        protected virtual string SanitizeResponseBody(HttpResponse response, string responseBody)
        {
            return responseBody;
        }

        private async Task<string> GetBodyAsync(Stream body, int? maxLength, string target)
        {
            _logger.LogTrace("Prepare for {Target} body to be tracked...", target);
            string sanitizedBody = await SanitizeStreamAsync(body, maxLength, target);
            if (sanitizedBody != null)
            {
                _logger.LogTrace("Found {Target} body to be tracked", target);
                return sanitizedBody;
            }
                
            _logger.LogWarning("No {Target} body was found to be tracked", target);
            return null;
        }

        private static async Task<string> SanitizeStreamAsync(Stream stream, int? maxLength, string targetName)
        {
            if (!stream.CanRead)
            {
                return $"{targetName} body could not be tracked because stream is not readable";
            }

            if (!stream.CanSeek)
            {
                return $"{targetName} body could not be tracked because stream is not seekable";
            }

            string contents;
            long? originalPosition = null;

            try
            {
                originalPosition = stream.Position;
                if (stream.Position != 0)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                var reader = new StreamReader(stream);
                if (maxLength.HasValue)
                {
                    var buffer = new char[maxLength.Value];
                    await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                    contents = new String(buffer); 
                }
                else
                {
                    contents = await reader.ReadToEndAsync();
                }
            }
            catch
            {
                // We don't want to track additional telemetry for cost purposes,
                // so we surface it like this
                contents = $"Unable to get '{targetName}' body for request tracking";
            }

            try
            {
                if (originalPosition.HasValue)
                {
                    stream.Seek(originalPosition.Value, SeekOrigin.Begin);
                }
            }
            catch
            {
                // Nothing to do here, we want to ensure the value is always returned.
            }

            // Trim string 'NULL' characters when the buffer was greater than the actual request/response body that was tracked.
            return contents?.TrimEnd('\0');
        }

        private bool AllowedToTrackStatusCode(HttpContext httpContext, IEnumerable<StatusCodeRange> attributeTrackedStatusCodes)
        {
            IEnumerable<HttpStatusCode> optionsTrackedStatusCodes = 
                Options.TrackedStatusCodes ?? Enumerable.Empty<HttpStatusCode>();

            IEnumerable<StatusCodeRange> optionsTrackedStatusCodeRanges =
                Options.TrackedStatusCodeRanges ?? Enumerable.Empty<StatusCodeRange>();
            
            StatusCodeRange[] combinedStatusCodeRanges = 
                optionsTrackedStatusCodes
                    .Select(code => new StatusCodeRange((int) code))
                    .Concat(optionsTrackedStatusCodeRanges)
                    .Concat(attributeTrackedStatusCodes)
                    .Where(range => range != null)
                    .Distinct().ToArray();

            bool allowedToTrackStatusCode = 
                combinedStatusCodeRanges.Length <= 0
                || combinedStatusCodeRanges.Any(range => range.IsWithinRange(httpContext.Response.StatusCode));

            string formattedStatusCodes = String.Join(", ", combinedStatusCodeRanges.Select(range => range.ToString()));
            if (allowedToTrackStatusCode)
            {
                _logger.LogTrace("Request tracking for this endpoint is allowed as the response status code '{ResponseStatusCode}' is within  the allowed tracked status code ranges '{TrackedStatusCodes}'", httpContext.Response.StatusCode, formattedStatusCodes);
            }
            else
            {
                _logger.LogTrace("Request tracking for this endpoint is disallowed as the response status code '{ResponseStatusCode}' is not within the allowed tracked status code ranges '{TrackedStatusCodes}'", httpContext.Response.StatusCode, formattedStatusCodes);
            }
            
            return allowedToTrackStatusCode;
        }

        private static async Task CopyTemporaryStreamToResponseStreamAsync(
            Stream temporaryResponseBodyStream,
            Stream originalResponseBodyStream)
        {
            temporaryResponseBodyStream.Position = 0;

            await temporaryResponseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }
}
