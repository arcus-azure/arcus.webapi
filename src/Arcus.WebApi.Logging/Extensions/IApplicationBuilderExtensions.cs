using System;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.Correlation;
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
        /// Adds the <see cref="ExceptionHandlingMiddleware"/> type to the application's request pipeline.
        /// </summary>
        /// <param name="app">The builder to configure the application's request pipeline.</param>
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            Guard.NotNull(app, nameof(app));

            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

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

        /// <summary>
        /// Adds operation and transaction correlation to the application by using the <see cref="CorrelationMiddleware"/> in the request pipeline.
        /// </summary>
        /// <param name="app">The builder to configure the application's request pipeline.</param>
        public static IApplicationBuilder UseHttpCorrelation(this IApplicationBuilder app)
        {
            Guard.NotNull(app, nameof(app));

            return app.UseMiddleware<CorrelationMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="VersionTrackingMiddleware"/> component to the application request's pipeline to automatically include the application version to the response.
        /// </summary>
        /// <param name="app">The builder to configure the application's request pipeline.</param>
        /// <param name="configureOptions">
        ///     The optional function to configure the version tracking options that will influence the behavior of the version tracking functionality.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="app"/> is <c>null</c>.</exception>
        /// <remarks>
        ///     WARNING: only use in non-publicly endpoints so the application version is not leaked and can be used for malicious purposes.
        /// </remarks>
        public static IApplicationBuilder UseVersionTracking(this IApplicationBuilder app, Action<VersionTrackingOptions> configureOptions = null)
        {
            Guard.NotNull(app, nameof(app), "Requires an application builder to add the version tracking middleware");

            var options = new VersionTrackingOptions();
            configureOptions?.Invoke(options);

            return app.UseMiddleware<VersionTrackingMiddleware>(options);
        }
    }
}