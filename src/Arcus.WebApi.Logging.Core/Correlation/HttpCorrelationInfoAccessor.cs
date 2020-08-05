﻿using System;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Arcus.WebApi.Logging.Correlation
{
    /// <summary>
    /// Represents the correlation information on the current HTTP request, accessible throughout the application.
    /// </summary>
    public class HttpCorrelationInfoAccessor : ICorrelationInfoAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelationInfoAccessor"/> class.
        /// </summary>
        /// <param name="contextAccessor">The instance to access the current <see cref="HttpContext"/>.</param>
        /// <param name="logger">The instance to log diagnostic messages during getting and settings of the <see cref="CorrelationInfo"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="contextAccessor"/> is <c>null</c>.</exception>
        public HttpCorrelationInfoAccessor(IHttpContextAccessor contextAccessor, ILogger<HttpCorrelationInfoAccessor> logger)
        {
            Guard.NotNull(contextAccessor, nameof(contextAccessor));
            Guard.NotNull(logger, nameof(logger));

            _httpContextAccessor = contextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelationInfoAccessor"/> class.
        /// </summary>
        public HttpCorrelationInfoAccessor(IHttpContextAccessor contextAccessor) 
            : this(contextAccessor, NullLogger<HttpCorrelationInfoAccessor>.Instance)
        {
        }

        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            IFeatureCollection features = _httpContextAccessor.HttpContext?.Features;
            if (features is null)
            {
                _logger.LogWarning("No HTTP context features available to retrieve the 'CorrelationInfo'");
            }

            var correlationInfo = features?.Get<CorrelationInfo>();
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
            IFeatureCollection features = _httpContextAccessor.HttpContext?.Features;
            if (features is null)
            {
                _logger.LogWarning("No HTTP context features available to set the 'CorrelationInfo'");
            }

            features?.Set(correlationInfo);
        }
    }
}
