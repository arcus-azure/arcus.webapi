using System;
using GuardNet;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Represents an endpoint attribute that indicates which endpoints should be withhold from request tracking.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ExcludeRequestTrackingAttribute : Attribute
    {
        private static readonly string ExcludeFilterNames = String.Join(", ", Enum.GetNames(typeof(ExcludeFilter)));

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeRequestTrackingAttribute" /> class
        /// that excludes by default the entire request from the telemetry tracking.
        /// </summary>
        public ExcludeRequestTrackingAttribute() : this(ExcludeFilter.ExcludeRoute)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeRequestTrackingAttribute" /> class
        /// that excludes only a partial of the request from the telemetry tracking.
        /// </summary>
        /// <param name="filter">The filter that describes what to exclude from the telemetry tracking.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="filter"/> is outside the range of the enumeration.</exception>
        public ExcludeRequestTrackingAttribute(ExcludeFilter filter)
        {
            Guard.For<ArgumentOutOfRangeException>(
                () => !Enum.IsDefined(typeof(ExcludeFilter), filter),
                $"Requires the exclusion filter to be within the bounds of the enumeration '{ExcludeFilterNames}'");

            Filter = filter;
        }

        /// <summary>
        /// Gets or sets the exclusion filter that determines which part of the request and response should be excluded from the request tracking.
        /// </summary>
        public ExcludeFilter Filter { get; }
    }
}
