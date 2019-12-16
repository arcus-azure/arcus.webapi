using Correlate.Http;

namespace Arcus.WebApi.Correlation 
{
    /// <summary>
    /// Options for handling correlation id on incoming requests.
    /// </summary>
    public class CorrelationOptions
    {
        /// <summary>
        /// Gets or sets the request headers to retrieve the correlation id from.
        /// </summary>
        /// <remarks>
        ///     The first matching header will be used.
        /// </remarks>
        public string[] RequestHeaders { get; set; } = { CorrelationHttpHeaders.CorrelationId, CorrelationHttpHeaders.RequestId };

        /// <summary>
        /// Gets the correlation options specific for the transaction ID.
        /// </summary>
        public CorrelationOptionsTransaction Transaction { get; } = new CorrelationOptionsTransaction();

        /// <summary>
        /// Gets the correlation options specific for the operation ID.
        /// </summary>
        public CorrelationOptionsOperation Operation { get; } = new CorrelationOptionsOperation();
    }
}