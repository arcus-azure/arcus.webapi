using GuardNet;
using Microsoft.AspNetCore.Http;

namespace Arcus.WebApi.Correlation
{
    /// <summary>
    /// Represents the correlation information on the current HTTP request, accessible throughout the application.
    /// </summary>
    public class CorrelationInfo
    {
        private readonly CorrelationFeature _correlationFeature;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationInfo"/> class.
        /// </summary>
        public CorrelationInfo(IHttpContextAccessor contextAccessor)
        {
            Guard.NotNull(contextAccessor, nameof(contextAccessor));

            _correlationFeature = contextAccessor.HttpContext.Features.Get<CorrelationFeature>();
        }

        /// <summary>
        /// Gets the unique ID identifier for this request.
        /// </summary>
        public string OperationId => _correlationFeature.OperationId;

        /// <summary>
        /// Gets the ID that relates different requests together.
        /// </summary>
        public string TransactionId => _correlationFeature.TransactionId;
    }
}
