using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging.Core.RequestTracking
{
    /// <summary>
    /// Represents a middleware template for tracking HTTP requests.
    /// </summary>
    public class RequestTrackingTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingTemplate"/> class.
        /// </summary>
        /// <param name="options">The additional options to configure the provided template functionality.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        protected RequestTrackingTemplate(RequestTrackingOptions options)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of additional user-configurable options to influence the behavior of the HTTP request tracking");
            Options = options;
        }

        /// <summary>
        /// Gets the consumer-configured options to control the behavior of the request tracking.
        /// </summary>
        public RequestTrackingOptions Options { get; }

        /// <summary>
        /// Determine whether or not a given <paramref name="requestPath"/> is allowed to be tracked by the HTTP request tracking <see cref="Options"/>.
        /// </summary>
        /// <param name="requestPath">The incoming HTTP request path.</param>
        /// <param name="logger">The logger to write diagnostic trace information during the determination of the HTTP request path.</param>
        protected bool IsRequestPathOmitted(PathString requestPath, ILogger logger)
        {
            logger = logger ?? NullLogger.Instance;

            IEnumerable<string> allOmittedRoutes = Options.OmittedRoutes ?? new Collection<string>();
            string[] matchedOmittedRoutes =
                allOmittedRoutes
                    .Select(omittedRoute => omittedRoute?.StartsWith("/") == true ? omittedRoute : "/" + omittedRoute)
                    .Where(omittedRoute => requestPath.StartsWithSegments(omittedRoute, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

            if (matchedOmittedRoutes.Length > 0)
            {
                string endpoint = requestPath.ToString();
                string formattedOmittedRoutes = string.Join(", ", matchedOmittedRoutes);
                logger.LogTrace("Skip request tracking for endpoint '{Endpoint}' due to an omitted route(s) '{OmittedRoutes}' specified in the options", endpoint, formattedOmittedRoutes);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determine whether the HTTP request should be tracked by verifying the outgoing HTTP response's <paramref name="responseStatusCode"/>.
        /// </summary>
        /// <param name="responseStatusCode">The outgoing HTTP response status code.</param>
        /// <param name="attributeTrackedStatusCodes">The additional set of allowed status codes that is also allowed.</param>
        /// <param name="logger">The logger instance to write diagnostic trace information during the determination.</param>
        protected bool AllowedToTrackStatusCode(int responseStatusCode, IEnumerable<StatusCodeRange> attributeTrackedStatusCodes, ILogger logger)
        {
            logger = logger ?? NullLogger.Instance;

            IEnumerable<HttpStatusCode> optionsTrackedStatusCodes =
                Options.TrackedStatusCodes ?? Enumerable.Empty<HttpStatusCode>();

            IEnumerable<StatusCodeRange> optionsTrackedStatusCodeRanges =
                Options.TrackedStatusCodeRanges ?? Enumerable.Empty<StatusCodeRange>();

            StatusCodeRange[] combinedStatusCodeRanges =
                optionsTrackedStatusCodes
                    .Select(code => new StatusCodeRange((int) code))
                    .Concat(attributeTrackedStatusCodes ?? Enumerable.Empty<StatusCodeRange>())
                    .Concat(optionsTrackedStatusCodeRanges)
                    .Where(range => range != null)
                    .Distinct()
                    .ToArray();

            bool allowedToTrackStatusCode =
                combinedStatusCodeRanges.Length <= 0
                || combinedStatusCodeRanges.Any(range => range.IsWithinRange(responseStatusCode));

            string formattedStatusCodes = string.Join(", ", combinedStatusCodeRanges.Select(range => range.ToString()));
            if (allowedToTrackStatusCode)
            {
                logger.LogTrace("Request tracking for this endpoint is allowed as the response status code '{ResponseStatusCode}' is within  the allowed tracked status code ranges '{TrackedStatusCodes}'", responseStatusCode, formattedStatusCodes);
            }
            else
            {
                logger.LogTrace("Request tracking for this endpoint is disallowed as the response status code '{ResponseStatusCode}' is not within the allowed tracked status code ranges '{TrackedStatusCodes}'", responseStatusCode, formattedStatusCodes);
            }

            return allowedToTrackStatusCode;
        }

        /// <summary>
        /// Creates a multi-dimensional telemetry context for HTTP request tracking.
        /// </summary>
        /// <param name="requestBody">The potential HTTP request body to be included in the telemetry context.</param>
        /// <param name="responseBody">The potential HTTP response body to be included in the telemetry context.</param>
        /// <param name="requestHeaders">The potential HTTP request headers to be included in the telemetry context.</param>
        /// <param name="logger">The logger instance to write failures during the sanitization of the HTTP <paramref name="requestHeaders"/>.</param>
        protected Dictionary<string, object> CreateTelemetryContext(string requestBody, string responseBody, IDictionary<string, StringValues> requestHeaders, ILogger logger)
        {
            logger = logger ?? NullLogger.Instance;

            try
            {
                IDictionary<string, StringValues> headers = GetSanitizedRequestHeaders(requestHeaders, logger);
                Dictionary<string, string> telemetryContext = headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value));

                if (string.IsNullOrWhiteSpace(requestBody) == false)
                {
                    telemetryContext.Add("RequestBody", requestBody);
                }

                if (string.IsNullOrWhiteSpace(responseBody) == false)
                {
                    telemetryContext.Add("ResponseBody", responseBody);
                }

                Dictionary<string, object> logContext = telemetryContext.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
                return logContext;
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "Failed to track request due to an unexpected failure");
            }

            return new Dictionary<string, object>();
        }

        private IDictionary<string, StringValues> GetSanitizedRequestHeaders(IDictionary<string, StringValues> headers, ILogger logger)
        {
            if (Options.IncludeRequestHeaders)
            {
                logger.LogTrace("Prepare for the request headers to be tracked...");
                IDictionary<string, StringValues> sanitizedHeaders = SanitizeRequestHeaders(headers);
                if (sanitizedHeaders != null && sanitizedHeaders.Any())
                {
                    string headerNames = String.Join(", ", sanitizedHeaders.Select(k => $"'{k.Key}'"));
                    logger.LogTrace("Found {RequestHeaders} request headers to be tracked", headerNames);

                    return sanitizedHeaders;
                }

                logger.LogWarning("No request headers were found to be tracked");
            }

            return new Dictionary<string, StringValues>();
        }

        /// <summary>
        /// Sanitize headers so that sensitive information is not logged via request tracking
        /// </summary>
        /// <param name="requestHeaders">The headers of the current HTTP request.</param>
        /// <remarks>Override this method if there are headers that contain sensitive information that should not be logged via request-tracking.</remarks>
        /// <returns>A collection of headers and the header contents that must be logged via request-tracking.</returns>
        protected virtual IDictionary<string, StringValues> SanitizeRequestHeaders(IDictionary<string, StringValues> requestHeaders)
        {
            if (requestHeaders.TryGetValue("value", out StringValues value))
            {
                requestHeaders["value"] = "<redacted>";
            }
            return requestHeaders.Where(header => Options.OmittedHeaderNames?.Contains(header.Key) == false);
        }

        /// <summary>
        /// Get the string representation of the streamed <paramref name="body"/>.
        /// </summary>
        /// <param name="body">The streaming body representing either incoming our outgoing messages.</param>
        /// <param name="maxLength">The maximum length the <paramref name="body"/> should be read.</param>
        /// <param name="targetName">The description of the <paramref name="body"/> (ex. Request or Response).</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the reading of the <paramref name="body"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="body"/> is <c>null</c>.</exception>
        protected async Task<string> GetBodyAsync(Stream body, int? maxLength, string targetName, ILogger logger)
        {
            Guard.NotNull(body, nameof(body), $"Requires a streamed body to read the string representation of the {targetName}");
            logger = logger ?? NullLogger.Instance;

            logger.LogTrace("Prepare for {Target} body to be tracked...", targetName);
            string sanitizedBody = await SanitizeStreamAsync(body, maxLength, targetName);
            if (sanitizedBody != null)
            {
                logger.LogTrace("Found {Target} body to be tracked", targetName);
                return sanitizedBody;
            }

            logger.LogWarning("No {Target} body was found to be tracked", targetName);
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
    }
}
