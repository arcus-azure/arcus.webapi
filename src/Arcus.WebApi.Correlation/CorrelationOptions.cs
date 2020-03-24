using System;
using Arcus.Observability.Correlation;

namespace Arcus.WebApi.Correlation
{
    /// <summary>
    /// Options for handling correlation id on incoming requests.
    /// </summary>
    [Obsolete("Correlation options is moved to 'Arcus.Observability.Correlation', use " + nameof(CorrelationInfoOptions) + " instead")]
    public class CorrelationOptions
    {
        /// <summary>
        /// Gets the correlation options specific for the transaction ID.
        /// </summary>
        public CorrelationOptionsTransaction Transaction { get; } = new CorrelationOptionsTransaction();

        /// <summary>
        /// Gets the correlation options specific for the operation ID.
        /// </summary>
        public CorrelationOptionsOperation Operation { get; } = new CorrelationOptionsOperation ();

        /// <summary>
        /// Transforms the old <see cref="CorrelationOptions"/> to the new <see cref="CorrelationInfoOptions"/>.
        /// </summary>
        /// <returns></returns>
        internal CorrelationInfoOptions ToCorrelationInfoOptions()
        {
            return new CorrelationInfoOptions
            {
                Operation =
                {
                    HeaderName = Operation.HeaderName,
                    GenerateId = Operation.GenerateId,
                    IncludeInResponse = Operation.IncludeInResponse
                },
                Transaction =
                {
                    HeaderName = Transaction.HeaderName,
                    AllowInRequest = Transaction.AllowInRequest,
                    IncludeInResponse = Transaction.IncludeInResponse,
                    GenerateId = Transaction.GenerateId,
                    GenerateWhenNotSpecified = Transaction.GenerateWhenNotSpecified
                }
            };
        }
    }
}
