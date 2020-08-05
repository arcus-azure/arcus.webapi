using System;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Arcus.WebApi.Logging.Correlation
{
    /// <summary>
    /// Represents the correlation information on the current HTTP request, accessible throughout the application.
    /// </summary>
    public class HttpCorrelationInfoAccessor : ICorrelationInfoAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelationInfoAccessor"/> class.
        /// </summary>
        /// <param name="contextAccessor">The instance to access the current <see cref="HttpContext"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="contextAccessor"/> is <c>null</c>.</exception>
        public HttpCorrelationInfoAccessor(IHttpContextAccessor contextAccessor)
        {
            Guard.NotNull(contextAccessor, nameof(contextAccessor));

            _httpContextAccessor = contextAccessor;
        }

        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            var correlationInfo = _httpContextAccessor.HttpContext?.Features?.Get<CorrelationInfo>();
            return correlationInfo;
        }

        /// <summary>
        /// Sets the current correlation information for this context.
        /// </summary>
        /// <param name="correlationInfo">The correlation model to set.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="correlationInfo"/> is <c>null</c>.</exception>
        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
            Guard.NotNull(correlationInfo, nameof(correlationInfo));
            _httpContextAccessor.HttpContext?.Features?.Set(correlationInfo);
        }
    }
}
