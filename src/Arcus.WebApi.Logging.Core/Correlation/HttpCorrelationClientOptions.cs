using System;
using System.Net.Http;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents additional user-configurable options to influence how the HTTP dependency is tracked via either the <see cref="HttpCorrelationMessageHandler"/>
    /// or via the <see cref="HttpClientExtensions.SendAsync(HttpClient,HttpRequestMessage,IHttpCorrelationInfoAccessor,ILogger)"/>.
    /// </summary>
    public class HttpCorrelationClientOptions
    {
        private Func<string> _generateDependencyId = () => Guid.NewGuid().ToString();
        private string _upstreamServiceHeaderName = "Request-Id";
        private string _transactionIdHeaderName = "X-Transaction-Id";

        /// <summary>
        /// Gets or sets the function to generate the dependency ID used when tracking HTTP dependencies.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="value"/> is <c>null</c>.</exception>
        public Func<string> GenerateDependencyId
        {
            get => _generateDependencyId;
            set
            {
                Guard.NotNull(value, nameof(value), "Requires a function to generate the dependency ID used when tracking HTTP dependencies");
                _generateDependencyId = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP request header name where the dependency ID (generated via <see cref="GenerateDependencyId"/>) should be added when tracking HTTP dependencies.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string UpstreamServiceHeaderName
        {
            get => _upstreamServiceHeaderName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank value for the HTTP request header where the dependency ID should be added when tracking HTTP dependencies");
                _upstreamServiceHeaderName = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP request header name where the transaction ID should be added when tracking HTTP dependencies.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string TransactionIdHeaderName
        {
            get => _transactionIdHeaderName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank value for the HTTP request header where the transaction ID should be added when tracking HTTP dependencies");
                _transactionIdHeaderName = value;
            }
        }
    }
}