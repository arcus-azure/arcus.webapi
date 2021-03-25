using System;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Represents an endpoint attribute that indicates which endpoints should be withhold from request tracking.
    /// </summary>
    public class SkipRequestTrackingAttribute : Attribute
    {
    }
}
