namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    ///  Options for handling correlation ID on incoming HTTP requests.
    /// </summary>
    public class HttpCorrelationInfoOptions 
    {
        /// <summary>
        /// Gets or sets the format within the Arcus HTTP correlation system will correlate HTTP requests.
        /// </summary>
        public HttpCorrelationFormat Format { get; set; } = HttpCorrelationFormat.W3C;

        /// <summary>
        /// Gets the correlation options specific for the operation ID.
        /// </summary>
        public HttpCorrelationInfoOperationOptions Operation { get; } = new HttpCorrelationInfoOperationOptions();

        /// <summary>
        /// Gets the correlation options specific for the transaction ID.
        /// </summary>
        public HttpCorrelationInfoTransactionOptions Transaction { get; } = new HttpCorrelationInfoTransactionOptions();

        /// <summary>
        /// Gets the correlation options specific for the upstream service.
        /// </summary>
        public CorrelationInfoUpstreamServiceOptions UpstreamService { get; } = new CorrelationInfoUpstreamServiceOptions();
    }
}
