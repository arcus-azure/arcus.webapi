using System;
using Arcus.Observability.Correlation;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    ///  Options for handling correlation ID on incoming HTTP requests.
    /// </summary>
    public class HttpCorrelationInfoOptions : CorrelationInfoOptions
    {
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
