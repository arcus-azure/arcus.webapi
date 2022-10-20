namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents the available HTTP correlation formats within the Arcus HTTP correlation system.
    /// </summary>
    public enum HttpCorrelationFormat
    {
        /// <summary>
        /// Uses the W3C HTTP Trace Context correlation system with traceparent and tracestate to represent parent-child relationship.
        /// </summary>
        W3C,

        /// <summary>
        /// Uses the hierarchical HTTP correlation system with Root-Id and Request-Id to represent parent-child relationship.
        /// </summary>
        Hierarchical
    }
}