using System;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.Net.Http.Headers;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Correlation options specific to the upstream services, used in the <see cref="CorrelationInfoOptions"/>.
    /// </summary>
    public class CorrelationInfoUpstreamServiceOptions
    {
        private string _headerName = HttpCorrelationProperties.UpstreamServiceHeaderName;
        private Func<string> _generateId = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the flag indicating whether or not the upstream service information should be extracted from the <see cref="OperationParentIdHeaderName"/> following the W3C Trace-Context standard. 
        /// </summary>
        /// <remarks>
        ///     This is only used when the <see cref="HttpCorrelationInfoOptions.Format"/> is set to <see cref="HttpCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        public bool ExtractFromRequest { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include the operation parent ID in the response.
        /// </summary>
        /// <remarks>
        ///     A common use case is to disable tracing info in edge services, so that such details are not exposed to the outside world.
        /// </remarks>
        public bool IncludeInResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets the request header name where te operation parent ID is located (default: <c>"Request-Id"</c>).
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        [Obsolete("Use '" + nameof(HeaderName) + "' instead")]
        public string OperationParentIdHeaderName
        {
            get => _headerName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank value for the operation parent ID request header name");
                _headerName = value;
            }
        }

        /// <summary>
        /// Gets or sets the request header name where te operation parent ID is located (default: <c>"Request-Id"</c>).
        /// </summary>
        /// <remarks>
        ///     Currently only used when the <see cref="HttpCorrelationInfoOptions.Format"/> is set to <see cref="HttpCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string HeaderName
        {
            get => _headerName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank value for the operation parent ID request header name");
                _headerName = value;
            }
        }

        /// <summary>
        /// Gets or sets the function to generate the operation parent ID without extracting from the request.
        /// </summary>
        /// <remarks>
        ///     This is only used when the <see cref="HttpCorrelationInfoOptions.Format"/> is set to <see cref="HttpCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        /// <exception cref="T:System.ArgumentNullException">Thrown when the <paramref name="value" /> is <c>null</c>.</exception>
        public Func<string> GenerateId
        {
            get => _generateId;
            set
            {
                Guard.NotNull(value, nameof (value), "Requires a function to generate the operation parent ID");
                _generateId = value;
            }
        }
    }
}
