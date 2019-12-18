using GuardNet;

namespace Arcus.WebApi.Correlation
{
    /// <summary>
    /// Correlation options specific for the operation ID.
    /// </summary>
    public class CorrelationOptionsOperation
    {
        private string _headerName = "X-Operation-ID";

        /// <summary>
        /// Gets or sets whether to include the operation ID in the response.
        /// </summary>
        /// <remarks>
        ///     A common use case is to disable tracing info in edge services, so that such details are not exposed to the outside world.
        /// </remarks>
        public bool IncludeInResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets the header that will contain the request/response operation ID.
        /// </summary>
        public string HeaderName
        {
            get => _headerName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Correlation operation header cannot be blank");
                _headerName = value;
            }
        }
    }
}
