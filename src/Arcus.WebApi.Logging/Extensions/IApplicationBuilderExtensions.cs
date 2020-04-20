using System;
using Arcus.WebApi.Logging;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extra extensions on the <see cref="IApplicationBuilder"/> for logging.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="RequestTrackingMiddleware"/> type to the application's request pipeline.
        /// </summary>
        /// <param name="app">The builder to configure the application's request pipeline.</param>
        /// <param name="configureOptions">The optional options to configure the behavior of the request tracking.</param>
        public static IApplicationBuilder UseRequestTracking(
            this IApplicationBuilder app,
            Action<RequestTrackingOptions> configureOptions = null)
        {
            Guard.NotNull(app, nameof(app));

            return UseRequestTracking<RequestTrackingMiddleware>(app, configureOptions);
        }

        /// <summary>
        /// Adds the <see cref="RequestTrackingMiddleware"/> type to the application's request pipeline.
        /// </summary>
        /// <param name="app">The builder to configure the application's request pipeline.</param>
        /// <param name="configureOptions">The optional options to configure the behavior of the request tracking.</param>
        public static IApplicationBuilder UseRequestTracking<TMiddleware>(
            this IApplicationBuilder app,
            Action<RequestTrackingOptions> configureOptions = null)
            where TMiddleware : RequestTrackingMiddleware
        {
            Guard.NotNull(app, nameof(app));

            var options = new RequestTrackingOptions();
            configureOptions?.Invoke(options);

            return app.UseMiddleware<TMiddleware>(options);
        }
    }
}
