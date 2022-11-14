using System;
using Arcus.Observability.Correlation;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    ///  Options for handling correlation ID on incoming HTTP requests.
    /// </summary>
#pragma warning disable CS0618
    public class HttpCorrelationInfoOptions : CorrelationInfoOptions
#pragma warning restore CS0618
    {
        /// <summary>
        /// Gets or sets the format within the Arcus HTTP correlation system will correlate HTTP requests.
        /// </summary>
        public HttpCorrelationFormat Format { get; set; } = HttpCorrelationFormat.W3C;

        /// <summary>
        /// Gets the correlation options specific for the operation ID.
        /// </summary>
        public new HttpCorrelationInfoOperationOptions Operation { get; } = new HttpCorrelationInfoOperationOptions();

        /// <summary>
        /// Gets the correlation options specific for the transaction ID.
        /// </summary>
        public new HttpCorrelationInfoTransactionOptions Transaction { get; } = new HttpCorrelationInfoTransactionOptions();

        /// <summary>
        /// Gets the correlation options specific for the upstream service.
        /// </summary>
        public CorrelationInfoUpstreamServiceOptions UpstreamService { get; } = new CorrelationInfoUpstreamServiceOptions();

        /// <summary/>
        [Obsolete("Use " + nameof(UpstreamService) + " instead")]
        public new Arcus.Observability.Correlation.CorrelationInfoUpstreamServiceOptions OperationParent { get; } = new Observability.Correlation.CorrelationInfoUpstreamServiceOptions();
    }
}
