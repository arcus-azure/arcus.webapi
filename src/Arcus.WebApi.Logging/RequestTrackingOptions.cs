using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Options that control the behavior in the <see cref="RequestTrackingMiddleware"/>.
    /// </summary>
    public class RequestTrackingOptions
    {
        /// <summary>
        /// Gets or sets the value indicating whether or not the HTTP request headers should be tracked.
        /// </summary>
        public bool IncludeRequestHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets the value to indicate whether or not the HTTP request body should be tracked.
        /// </summary>
        public bool IncludeRequestBody { get; set; } = false;

        /// <summary>
        /// Gets or sets the HTTP request headers names that will be omitted during request tracking.
        /// </summary>
        public ICollection<string> OmittedHeaderNames { get; set; } = new Collection<string> { "Authentication", "X-Api-Key", "X-ARR-ClientCert" };
    }
}
