namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents constant properties within HTTP correlation scenarios.
    /// </summary>
    public static class HttpCorrelationProperties
    {
        /// <summary>
        /// Gets the default HTTP request header name used to set the transaction ID in HTTP correlation scenarios.
        /// </summary>
        public const string TransactionIdHeaderName = "X-Transaction-ID";

        /// <summary>
        /// Gets the default HTTP request header name used to set the upstream service ID in HTTP correlation scenarios.
        /// </summary>
        public const string UpstreamServiceHeaderName = "Request-Id";
    }
}
