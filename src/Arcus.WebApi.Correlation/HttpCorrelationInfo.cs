using GuardNet;
using Microsoft.AspNetCore.Http;

namespace Arcus.WebApi.Correlation
{
    /// <summary>
    /// Represents the correlation information on the current HTTP request, accessible throughout the application.
    /// </summary>
    public class HttpCorrelationInfo
    {
        private readonly CorrelationInfo _correlationInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelationInfo"/> class.
        /// </summary>
        public HttpCorrelationInfo(IHttpContextAccessor contextAccessor)
        {
            Guard.NotNull(contextAccessor, nameof(contextAccessor));

            _correlationInfo = contextAccessor.HttpContext.Features.Get<CorrelationInfo>();
        }

        /// <summary>
        /// Gets the unique ID identifier for this request.
        /// </summary>
        public string OperationId => _correlationInfo.OperationId;

        /// <summary>
        /// Gets the ID that relates different requests together.
        /// </summary>
        public string TransactionId => _correlationInfo.TransactionId;
    }
}
