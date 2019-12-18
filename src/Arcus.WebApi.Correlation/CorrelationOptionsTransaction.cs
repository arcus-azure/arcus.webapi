using GuardNet;

namespace Arcus.WebApi.Correlation
{
    /// <summary>
    /// Correlation options specific to the transaction ID.
    /// </summary>
    public class CorrelationOptionsTransaction
    {
        private string _headerName = "X-Transaction-ID";

        /// <summary>
        /// Get or sets whether the transaction ID can be specified in the request, and will be used throughout the request handling.
        /// </summary>
        public bool AllowInRequest { get; set; } = true;

        /// <summary>
        /// Gets or sets whether or not the transaction ID should be generated when there isn't any transaction ID found in the request.
        /// </summary>
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
    }
}
