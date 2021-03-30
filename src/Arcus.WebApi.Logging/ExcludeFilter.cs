﻿using System;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Represents a filter that determines what part of the request and response should be excluded from the request tracking while using the <see cref="ExcludeRequestTrackingAttribute"/>.
    /// </summary>
    [Flags]
    public enum ExcludeFilter
    {
        /// <summary>
        /// Specifies that nothing will be excluded from the request tracking.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Specifies that the request body will be excluded from the request tracking.
        /// </summary>
        ExcludeRequestBody = 1,

        /// <summary>
        /// Specifies that the response body will be excluded from the request tracking.
        /// </summary>
        ExcludeResponseBody = 2,
        
        /// <summary>
        /// Specifies that the entire endpoint should be excluded from the request tracking.
        /// </summary>
        ExcludeRoute = 7
    }
}