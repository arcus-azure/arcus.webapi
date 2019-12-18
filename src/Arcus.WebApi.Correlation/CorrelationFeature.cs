using System;
using GuardNet;

namespace Arcus.WebApi.Correlation
{
    /// <summary>
    /// Represents the correlation ID information on the incoming requests and outgoing responses.
    /// </summary>
    internal class Correlation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationInfo"/> class.
        /// </summary>
        internal Correlation(string operationId, string transactionId)
        {
            Guard.NotNullOrEmpty(operationId, nameof(operationId), "Cannot create a correlation instance with a blank operation ID");
            Guard.For<ArgumentException>(
                () => transactionId == String.Empty, 
                "Cannot create correlation instance with a blank transaction ID, only 'null' or non-blank ID's are allowed");

            OperationId = operationId;
            TransactionId = transactionId;
        }

        /// <summary>
        /// Gets the ID that relates different requests together.
        /// </summary>
        internal string TransactionId { get; }

        /// <summary>
        /// Gets the unique ID information of the request.
        /// </summary>
        internal string OperationId { get; }
    }
}
