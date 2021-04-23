using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using GuardNet;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Options that control the behavior in the <see cref="RequestTrackingMiddleware"/>.
    /// </summary>
    public class RequestTrackingOptions
    {
        private int? _requestBodyBufferSize,
                     _responseBodyBufferSize;

        /// <summary>
        /// Gets or sets the value indicating whether or not the HTTP request headers should be tracked.
        /// </summary>
        public bool IncludeRequestHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets the value to indicate whether or not the HTTP request body should be tracked.
        /// </summary>
        public bool IncludeRequestBody { get; set; } = false;

        /// <summary>
        /// Gets or sets the size (in bytes) of the request body buffer which indicates the maximum length of the body that should be tracked.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        public int? RequestBodyBufferSize
        {
            get => _requestBodyBufferSize;
            set
            {
                Guard.For<ArgumentOutOfRangeException>(() => value < 0, "Requires a request body buffer size greater than zero");
                _requestBodyBufferSize = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the value to indicate whether or not the HTTP response body should be tracked.
        /// </summary>
        public bool IncludeResponseBody { get; set; } = false;

        /// <summary>
        /// Gets or sets the size (in bytes) of the response body buffer which indicates the maximum length of the body that should be tracked.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        public int? ResponseBodyBufferSize
        {
            get => _responseBodyBufferSize;
            set
            {
                Guard.For<ArgumentOutOfRangeException>(() => value < 0, "Requires a response body buffer size greater than zero");
                _responseBodyBufferSize = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the HTTP response status codes that should be tracked. If not defined, all HTTP status codes are considered included and will all be tracked.
        /// </summary>
        public ICollection<HttpStatusCode> TrackedStatusCodes { get; set; } = new Collection<HttpStatusCode>();

        /// <summary>
        /// Gets or sets the HTTP request headers names that will be omitted during request tracking.
        /// </summary>
        public ICollection<string> OmittedHeaderNames { get; set; } = new Collection<string> { "Authentication", "Authorization", "X-Api-Key", "X-ARR-ClientCert", "Ocp-Apim-Subscription-Key" };

        /// <summary>
        /// Gets or sets the HTTP endpoint routes that will be omitted during request tracking.
        /// </summary>
        public ICollection<string> OmittedRoutes { get; set; } = new Collection<string>();
    }
}
