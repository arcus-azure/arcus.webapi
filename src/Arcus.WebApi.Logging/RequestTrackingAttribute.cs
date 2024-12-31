using System;
using System.Linq;
using System.Net;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Specifies additional configuration for this endpoint during request tracking.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequestTrackingAttribute : Attribute
    {
        private static readonly string ExcludeFilterNames = String.Join(", ", Enum.GetNames(typeof(Exclude)).Where(name => name != Exclude.None.ToString()));
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeRequestTrackingAttribute" /> class
        /// that excludes only a partial of the request from the telemetry tracking.
        /// </summary>
        /// <param name="filter">The filter that describes what to exclude from the telemetry tracking.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="filter"/> is outside the range of the enumeration.</exception>
        public RequestTrackingAttribute(Exclude filter)
        {
            if (!Enum.IsDefined(typeof(Exclude), filter) || filter is Exclude.None)
            {
                throw new ArgumentOutOfRangeException(nameof(filter), $"Requires the exclusion filter to be within these bounds of the enumeration '{ExcludeFilterNames}'; 'None' is not allowed",);
            }

            Filter = filter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingAttribute" /> class.
        /// </summary>
        /// <param name="trackedStatusCode">The HTTP response status code that should be tracked.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="trackedStatusCode"/> is outside the expected range (100-599).</exception>
        public RequestTrackingAttribute(HttpStatusCode trackedStatusCode) : this((int) trackedStatusCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingAttribute" /> class.
        /// </summary>
        /// <param name="trackedStatusCode">The HTTP response status code that should be tracked.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="trackedStatusCode"/> is outside the expected range (100-599).</exception>
        public RequestTrackingAttribute(int trackedStatusCode)
        {
            if (trackedStatusCode < 100 || trackedStatusCode > 599)
            {
                throw new ArgumentOutOfRangeException(nameof(trackedStatusCode), "Requires the allowed tracked HTTP status code to be within the range of 100-599");
            }
            
            StatusCodeRange = new StatusCodeRange(trackedStatusCode);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingAttribute" /> class.
        /// </summary>
        /// <param name="minimumStatusCode">The minimum HTTP status code threshold of the range of allowed tracked HTTP status codes.</param>
        /// <param name="maximumStatusCode">The maximum HTTP status code threshold of the range of allowed tracked HTTP status codes.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="minimumStatusCode"/> is less than 100,
        ///     or the <paramref name="maximumStatusCode"/> is greater than 599,
        ///     or the <paramref name="minimumStatusCode"/> is greater than the <paramref name="maximumStatusCode"/>.
        /// </exception>
        public RequestTrackingAttribute(int minimumStatusCode, int maximumStatusCode)
        {
            if (minimumStatusCode < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumStatusCode), "Requires the minimum HTTP status code threshold to not be less than 100");
            }
            if (maximumStatusCode > 599)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumStatusCode), "Requires the maximum HTTP status code threshold to not be greater than 599");
            }
            if (minimumStatusCode >= maximumStatusCode)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumStatusCode), "Requires the minimum HTTP status code threshold to be less than the maximum HTTP status code threshold");
            }
            
            StatusCodeRange = new StatusCodeRange(minimumStatusCode, maximumStatusCode);
        }

        /// <summary>
        /// Gets or sets the exclusion filter that determines which part of the request and response should be excluded from the request tracking.
        /// </summary>
        public Exclude Filter { get; } = Exclude.None;

        /// <summary>
        /// Gets the HTTP status code ranges that are allowed to be tracked. IF not defined, all HTTP status codes are considered included and will all be tracked.
        /// </summary>
        public StatusCodeRange StatusCodeRange { get; }
    }
}