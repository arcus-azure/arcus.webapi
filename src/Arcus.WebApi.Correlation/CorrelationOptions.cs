namespace Arcus.WebApi.Correlation 
{
    /// <summary>
    /// Options for handling correlation id on incoming requests.
    /// </summary>
    public class CorrelationOptions
    {
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