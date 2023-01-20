using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using GuardNet;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Specifies additional configuration for this endpoint during request tracking.
    /// </summary>
    [ExcludeFromCodeCoverage]
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
            Guard.For<ArgumentOutOfRangeException>(
                () => !Enum.IsDefined(typeof(Exclude), filter) || filter is Exclude.None,
                $"Requires the exclusion filter to be within these bounds of the enumeration '{ExcludeFilterNames}'; 'None' is not allowed");

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
            Guard.NotLessThan(trackedStatusCode, 100, nameof(trackedStatusCode), "Requires the allowed tracked HTTP status code to not be less than 100");
            Guard.NotGreaterThan(trackedStatusCode, 599, nameof(trackedStatusCode), "Requires the allowed tracked HTTP status code to not be greater than 599");
            
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
            Guard.NotLessThan(minimumStatusCode, 100, nameof(minimumStatusCode), "Requires the minimum HTTP status code threshold not be less than 100");
            Guard.NotGreaterThan(maximumStatusCode, 599, nameof(maximumStatusCode), "Requires the maximum HTTP status code threshold not be greater than 599");
            Guard.NotGreaterThan(minimumStatusCode, maximumStatusCode, nameof(minimumStatusCode), "Requires the minimum HTTP status code threshold to be less than the maximum HTTP status code threshold");
            
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