using System;
using GuardNet;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents the correlation options specific for the transaction ID.
    /// </summary>
    public class HttpCorrelationInfoTransactionOptions
    {
        private string _headerName = HttpCorrelationProperties.TransactionIdHeaderName;
        private Func<string> _generateId = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Get or sets whether the transaction ID can be specified in the request, and will be used throughout the request handling.
        /// </summary>
        /// <remarks>
        ///     This is only used when the <see cref="HttpCorrelationInfoOptions.Format"/> is set to <see cref="HttpCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        public bool AllowInRequest { get; set; } = true;

        /// <summary>
        /// Gets or sets whether or not the transaction ID should be generated when there isn't any transaction ID found in the request.
        /// </summary>
        /// <remarks>
        ///     This is only used when the <see cref="HttpCorrelationInfoOptions.Format"/> is set to <see cref="HttpCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        public bool GenerateWhenNotSpecified { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include the transaction ID in the response.
        /// </summary>
        /// <remarks>
        ///     A common use case is to disable tracing info in edge services, so that such details are not exposed to the outside world.
        /// </remarks>
        public bool IncludeInResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets the header that will contain the request/response transaction ID.
        /// </summary>
        public string HeaderName
        {
            get => _headerName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Correlation transaction header cannot be blank");
                _headerName = value;
            }
        }

        /// <summary>
        /// Gets or sets the function to generate the transaction ID when
        /// (1) the request doesn't have already an transaction ID, and
        /// (2) the <see cref="P:Arcus.Observability.Correlation.CorrelationInfoTransactionOptions.IncludeInResponse" /> is set to <c>true</c> (default: <c>true</c>), and
        /// (3) the <see cref="P:Arcus.Observability.Correlation.CorrelationInfoTransactionOptions.GenerateWhenNotSpecified" /> is set to <c>true</c> (default: <c>true</c>).
        /// </summary>
        /// <remarks>
        ///     This is only used when the <see cref="HttpCorrelationInfoOptions.Format"/> is set to <see cref="HttpCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        public Func<string> GenerateId
        {
            get => this._generateId;
            set
            {
                Guard.NotNull(value, nameof(value), "Correlation function to generate an transaction ID cannot be 'null'");
                _generateId = value;
            }
        }
    }
}