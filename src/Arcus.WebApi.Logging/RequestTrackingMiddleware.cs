﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;
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
        /// <param name="logger">The logger to write diagnostic messages during the request tracking.</param>
        public RequestTrackingMiddleware(
            RequestTrackingOptions options,
            RequestDelegate next,
            ILogger<RequestTrackingMiddleware> logger)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(next, nameof(next));
            Guard.NotNull(logger, nameof(logger));

            _options = options;
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Logs every incoming HTTP request.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        public async Task Invoke(HttpContext httpContext)
        {
            var stopwatch = Stopwatch.StartNew();
            if (_options.IncludeRequestBody)
            {
                httpContext.Request.EnableBuffering();
            }

            try
            {
                await _next(httpContext);
            }
            finally
            {
                stopwatch.Stop();
                await TrackRequestAsync(httpContext, stopwatch.Elapsed);
            }
        }

        private async Task TrackRequestAsync(HttpContext httpContext, TimeSpan duration)
        {
            try
            {
                IDictionary<string, StringValues> telemetryContext = GetRequestHeaders(httpContext);

                string requestBody = await GetRequestBodyAsync(httpContext);
                if (requestBody != null)
                {
                    telemetryContext.Add("Body", requestBody);
                }

                Dictionary<string, object> logContext = telemetryContext.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
                _logger.LogRequest(httpContext.Request, httpContext.Response, duration, logContext);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failed to track request");
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
                _logger.LogTrace("Prepare for the request body to be tracked...");
                string sanitizedBody = await SanitizeRequestBodyAsync(httpContext.Request.Body);
                if (sanitizedBody != null)
                {
                    _logger.LogTrace("Found request body to be tracked");
                    return sanitizedBody;
                }

                _logger.LogWarning("No request body was found to be tracked");
            }

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

        private static async Task<string> SanitizeRequestBodyAsync(Stream requestStream)
        {
            if (!requestStream.CanRead)
            {
                return "Request body could not be tracked because stream is not readable";
            }

            if (!requestStream.CanSeek)
            {
                return "Request body could not be tracked because stream is not seekable";
            }

            long originalPosition = requestStream.Position;
            if (requestStream.Position != 0)
            {
                requestStream.Seek(0, SeekOrigin.Begin);
            }

            var reader = new StreamReader(requestStream);
            string contents = await reader.ReadToEndAsync();

            try
            {
                requestStream.Seek(originalPosition, SeekOrigin.Begin);
            }
            catch
            {
                // Nothing to do here, we want to ensure the value is always returned.
            }

            return contents;
        }
    }
}
