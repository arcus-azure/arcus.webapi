using System;
using System.Linq;
using System.Net;
using GuardNet;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Represents an endpoint attribute that indicates which endpoints should be withhold from request tracking.
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
            Guard.For<ArgumentOutOfRangeException>(
                () => !Enum.IsDefined(typeof(Exclude), filter) || filter is Exclude.None,
                $"Requires the exclusion filter to be within these bounds of the enumeration '{ExcludeFilterNames}'; 'None' is not allowed");

            Filter = filter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingAttribute" /> class.
        /// </summary>
        public RequestTrackingAttribute(params HttpStatusCode[] trackedStatusCodes)
        {
            TrackedStatusCodes = trackedStatusCodes;
        }

        /// <summary>
        /// Gets or sets the exclusion filter that determines which part of the request and response should be excluded from the request tracking.
        /// </summary>
        public Exclude Filter { get; } = Exclude.None;

        /// <summary>
        /// 
        /// </summary>
        public HttpStatusCode[] TrackedStatusCodes { get; } = Array.Empty<HttpStatusCode>();
    }
}