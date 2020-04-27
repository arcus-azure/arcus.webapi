using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
                IDictionary<string, StringValues> telemetryContext = new Dictionary<string, StringValues>();
                if (_options.IncludeRequestHeaders)
                {
                    IDictionary<string, StringValues> sanitizedHeaders = SanitizeRequestHeaders(httpContext.Request.Headers);
                    if (sanitizedHeaders != null)
                    {
                        telemetryContext = sanitizedHeaders;
                    }
                }

                if (_options.IncludeRequestBody)
                {
                    string sanitizedBody = await SanitizeRequestBodyAsync(httpContext.Request.Body);
                    if (sanitizedBody != null)
                    {
                        telemetryContext.Add("Body", sanitizedBody);
                    }
                }

                Dictionary<string, object> logContext = telemetryContext.ToDictionary(kv => kv.Key, kv => (object) kv.Value);
                _logger.LogRequest(httpContext.Request, httpContext.Response, duration, logContext);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failed to track request");
            }
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
                return null;
            }

            if (requestStream.CanSeek)
            {
                if (requestStream.Position != 0)
                {
                    requestStream.Position = 0;
                }
            }
            else
            {
                try
                {
                    if (requestStream.Position != 0)
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }

            using (var reader = new StreamReader(requestStream))
            {
                string contents = await reader.ReadToEndAsync();
                return contents;
            }
        }
    }
}
