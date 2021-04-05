using System;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Represents a filter that determines what part of the request and response should be excluded from the request tracking while using the <see cref="RequestTrackingAttribute"/>.
    /// </summary>
    [Flags]
    public enum Exclude
    {
        /// <summary>
        /// Specifies that nothing will be excluded from the request tracking.
        /// </summary>
        /// <remarks>
        ///     This enumeration member value is normally not used within normal circumstances as it indicates that the request tracking will not exclude anything.
        /// </remarks>
        None = 0,
        
        /// <summary>
        /// Specifies that the request body will be excluded from the request tracking.
        /// </summary>
        RequestBody = 1,

        /// <summary>
        /// Specifies that the response body will be excluded from the request tracking.
        /// </summary>
        ResponseBody = 2
    }
}