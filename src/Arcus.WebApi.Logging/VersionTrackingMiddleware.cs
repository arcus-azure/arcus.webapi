using System;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Serilog.Enrichers;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Version tracking middleware component to automatically add the version of the application to the response.
    /// </summary>
    /// <remarks>
    ///     WARNING: Only use the version tracking for non-public endpoints otherwise the version information is leaked and it can be used for unintended malicious purposes.
    /// </remarks>
    public class VersionTrackingMiddleware
    {
        private readonly IAppVersion _appVersion;
        private readonly VersionTrackingOptions _options;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionTrackingMiddleware"/> class.
        /// </summary>
        /// <param name="appVersion">The instance to retrieve the current application version.</param>
        /// <param name="options">The configurable options to specify how the version should be tracked in the response.</param>
        /// <param name="next">The next functionality in the request pipeline to be executed.</param>
        /// <param name="logger">The logger to write diagnostic trace messages during the addition of the application version to the response.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="appVersion"/>, <paramref name="options"/>, or <paramref name="next"/> is <c>null</c>.</exception>
        public VersionTrackingMiddleware(
            IAppVersion appVersion,
            VersionTrackingOptions options,
            RequestDelegate next,
            ILogger<VersionTrackingMiddleware> logger)
        {
            Guard.NotNull(appVersion, nameof(appVersion), "Requires an instance to retrieve the current application version to add the version to the response");
            Guard.NotNull(next, nameof(next), "Requires a continuation delegate to move towards the next functionality in the request pipeline");
            Guard.NotNull(options, nameof(options), "Requires version tracking options to specify how the application version should be tracked in the response");

            _appVersion = appVersion;
            _options = options;
            _next = next;
            _logger = logger ?? NullLogger<VersionTrackingMiddleware>.Instance;
        }

        /// <summary>
        /// Invoke the middleware to include the version of the application.
        /// </summary>
        /// <param name="context">The context for the current HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="context"/> doesn't contain a response.</exception>
        public async Task Invoke(HttpContext context)
        {
            Guard.NotNull(context, nameof(context), "Requires a HTTP context to add the application version to the response");
            Guard.For(() => context.Response is null, new ArgumentException("Requires a HTTP context with a response to add the application version", nameof(context)));

            context.Response.OnStarting(() =>
            {
                string version = _appVersion.GetVersion();
                if (String.IsNullOrWhiteSpace(version))
                {
                    _logger.LogWarning("Setting current application version halted because the '{Type}' got a blank version", _appVersion.GetType().Name);
                }
                else
                {
                    _logger.LogTrace("Setting current application version response header '{HeaderName}' to '{Version}'", _options.HeaderName, version);
                    context.Response.Headers[_options.HeaderName] = version;
                }

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
