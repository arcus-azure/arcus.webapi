using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// 
    /// </summary>
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the request tracing middleware component <see cref="RequestTrackingMiddleware"/> to the application to log every incoming HTTP request.
        /// </summary>
        /// <param name="app">THe current HTTP context.</param>
        /// <param name="extractHeaders"></param>
        /// <param name="extractBody"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRequestTracking(
            this IApplicationBuilder app, 
            Func<IHeaderDictionary, IDictionary<string, object>> extractHeaders = null,
            Func<HttpRequest, IDictionary<string, object>> extractBody = null)
        {
            return app.UseMiddleware<RequestTrackingMiddleware>(extractHeaders, extractBody);
        }
    }
        
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
        /// <param name="next">The next pipeline function to process the HTTP context.</param>
        /// <param name="logger">The logger to write diagnostic messages during the request tracking.</param>
        public RequestTrackingMiddleware(
            RequestDelegate next,
            ILogger<RequestTrackingMiddleware> logger)
        {
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
                var headers = ExtractRequestHeaders(httpContext.Request.Headers) ?? new Dictionary<string, object>();
                var body = await ExtractRequestBodyAsync(httpContext.Request.Body) ?? new Dictionary<string, object>();
                Dictionary<string, object> logContext = headers.Concat(body).ToDictionary(kv => kv.Key, kv => kv.Value);

                _logger.LogRequest(httpContext.Request, httpContext.Response, duration, logContext);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failed to track request");
            }
        }

        protected virtual IDictionary<string, object> ExtractRequestHeaders(IHeaderDictionary requestHeaders)
        {
            return requestHeaders.ToDictionary(header => header.Key, header => (object) header.Value);
        }

        protected virtual async Task<IDictionary<string, object>> ExtractRequestBodyAsync(Stream requestStream)
        {
            if (!requestStream.CanRead)
            {
                return new Dictionary<string, object>();
            }

            if (requestStream.CanSeek)
            {
                if (requestStream.Position > 0)
                {
                    requestStream.Position = 0;
                }
            }

            using (var reader = new StreamReader(requestStream))
            {
                string contents = await reader.ReadToEndAsync();
                return new Dictionary<string, object>
                {
                    ["Body"] = contents
                };
            }
        }
    }
}
