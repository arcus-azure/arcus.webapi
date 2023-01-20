using System.Diagnostics.CodeAnalysis;
using GuardNet;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Represents the user-configurable options to control how the <see cref="VersionTrackingMiddleware"/> should track the current application version in the response.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class VersionTrackingOptions
    {
        private string _headerName = "X-Version";

        /// <summary>
        /// Gets or sets the header name on which the current application version should be added.
        /// </summary>
        public string HeaderName
        {
            get => _headerName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank header name to add the current application version to the response");
                _headerName = value;
            }
        }
    }
}