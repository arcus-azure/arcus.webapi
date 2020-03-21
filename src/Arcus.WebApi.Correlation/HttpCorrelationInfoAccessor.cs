using System;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;

namespace Arcus.WebApi.Correlation
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
        public HttpCorrelationInfoAccessor(IHttpContextAccessor contextAccessor)
        {
            Guard.NotNull(contextAccessor, nameof(contextAccessor));

            _httpContextAccessor = contextAccessor;
        }

        /// <summary>
        /// Gets or sets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo CorrelationInfo
        {
            get => _httpContextAccessor.HttpContext.Features.Get<CorrelationInfo>();
            set => throw new NotSupportedException(
                "The correlation information is automatically set during the application middleware and is not supported to be altered afterwards");
        }
    }
}
