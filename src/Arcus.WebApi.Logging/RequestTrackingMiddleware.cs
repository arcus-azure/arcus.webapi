using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Request tracing middleware component to log every incoming HTTP request.
    /// </summary>
    public class RequestTrackingMiddleware
    {
        private readonly RequestTrackingOptions _options;
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

            _options = options;
            _next = next;
            _logger = logger;
        }

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

            var endpoint = httpContext.Features.Get<IEndpointFeature>();
            if (endpoint is null)
            {
                _logger.LogWarning(
                    "Cannot determine whether or not the endpoint contains the '{Attribute}' because the endpoint tracking (`IApplicationBuilder.UseRouting()` or `.UseEndpointRouting()`) was not activated before the request tracking middleware",
                    nameof(SkipRequestTrackingAttribute));
            }
            
            var skipRequestTrackingAttribute = endpoint?.Endpoint.Metadata.GetMetadata<SkipRequestTrackingAttribute>();
            if (skipRequestTrackingAttribute != null)
            {
                _logger.LogTrace("Skip request tracking for endpoint '{Endpoint}' due to the '{Attribute}' attribute on the endpoint", endpoint.Endpoint.DisplayName, nameof(SkipRequestTrackingAttribute));
                await _next(httpContext);
            }
            else
            {
                await TrackRequest(httpContext);
            }
        }

        private async Task TrackRequest(HttpContext httpContext)
        {
            var stopwatch = Stopwatch.StartNew();

            string requestBody = null;
            if (_options.IncludeRequestBody)
            {
                httpContext.Request.EnableBuffering();
                requestBody = await GetRequestBodyAsync(httpContext);
            }

            // Response body doesn't support (built-in) buffering and is not seekable, so we're storing temporary the response stream in our own seekable stream,
            // which we later (*) replace back with the original response stream.
            // If we don't store it in our own seekable stream first, we would read the response stream for tracking and could not use the same stream to respond to the request.
            
            Stream originalResponseBodyStream = null;
            using (Stream temporaryResponseBodyStream = DetermineResponseBodyBuffer())
            {
                if (_options.IncludeResponseBody)
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
                    string responseBody = await GetResponseBodyAsync(httpContext);

                    stopwatch.Stop();
                    LogRequest(requestBody, responseBody, httpContext, stopwatch.Elapsed);

                    if (_options.IncludeResponseBody)
                    {
                        // (*) Copy back the seekable/temporary response body stream to the original response body stream,
                        // for the remaining middleware components that comes after this one.
                        await CopyTemporaryStreamToResponseStreamAsync(temporaryResponseBodyStream, originalResponseBodyStream);
                    }
                }
            }
        }

        private Stream DetermineResponseBodyBuffer()
        {
            if (_options.IncludeResponseBody)
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
            if (_options.IncludeRequestHeaders)
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

        private async Task<string> GetRequestBodyAsync(HttpContext httpContext)
        {
            if (_options.IncludeRequestBody)
            {
                string sanitizedBody = await GetBodyAsync(httpContext.Request.Body, _options.RequestBodyBufferSize, "Request");
                return sanitizedBody;
            }

            return null;
        }

        private async Task<string> GetResponseBodyAsync(HttpContext httpContext)
        {
            if (_options.IncludeResponseBody)
            {
                string sanitizedBody = await GetBodyAsync(httpContext.Response.Body, _options.ResponseBodyBufferSize, "Response");
                return sanitizedBody;
            }

            return null;
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

        /// <summary>
        /// Extracts information from the given HTTP <paramref name="requestHeaders"/> to include in the request tracking context.
        /// </summary>
        /// <param name="requestHeaders">The headers of the current HTTP request.</param>
        protected virtual IDictionary<string, StringValues> SanitizeRequestHeaders(IDictionary<string, StringValues> requestHeaders)
        {
            return requestHeaders.Where(header => _options.OmittedHeaderNames.Contains(header.Key) == false);
        }

        private async Task<string> SanitizeStreamAsync(Stream stream, int? maxLength, string targetName)
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

        private static async Task CopyTemporaryStreamToResponseStreamAsync(
            Stream temporaryResponseBodyStream,
            Stream originalResponseBodyStream)
        {
            temporaryResponseBodyStream.Position = 0;

            await temporaryResponseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }
}
