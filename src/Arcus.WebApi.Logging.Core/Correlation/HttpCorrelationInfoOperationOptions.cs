using System;
using GuardNet;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents the correlation options specific for the operation ID.
    /// </summary>
    public class HttpCorrelationInfoOperationOptions
    {
        private string _headerName = "X-Operation-ID";
        private Func<string> _generateId = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets whether to include the operation ID in the response.
        /// </summary>
        /// <remarks>
        ///     A common use case is to disable tracing info in edge services, so that such details are not exposed to the outside world.
        /// </remarks>
        [Obsolete("Operation ID's should not be available in response, use the " + nameof(HttpCorrelationInfoOptions.UpstreamService) + " to include correlation ID's")]
        public bool IncludeInResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets the header that will contain the response operation ID.
        /// </summary>
        [Obsolete("Operation ID's should not be available in response, use the " + nameof(HttpCorrelationInfoOptions.UpstreamService) + " to include correlation ID's")]
        public string HeaderName
        {
            get => _headerName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof (value), "Correlation operation header cannot be blank");
                _headerName = value;
            }
        }

        /// <summary>
        /// Gets or sets the function to generate the operation ID when the <see cref="P:Arcus.Observability.Correlation.CorrelationInfoOperationOptions.IncludeInResponse" /> is set to <c>true</c> (default: <c>true</c>).
        /// </summary>
        public Func<string> GenerateId
        {
            get => _generateId;
            set
            {
                Guard.NotNull(value, nameof (value), "Correlation function to generate an operation ID cannot be 'null'");
                _generateId = value;
            }
        }
    }
}