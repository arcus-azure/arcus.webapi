using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds operation and transaction correlation to the application.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds operation and transaction correlation to the application.
        /// </summary>
        /// <param name="services">The services collection containing the dependency injection services.</param>
        public static IServiceCollection AddHttpCorrelation(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the HTTP correlation services");

            return AddHttpCorrelation(services, configureOptions: (HttpCorrelationInfoOptions options) => { });
        }

        /// <summary>
        /// Adds operation and transaction correlation to the application.
        /// </summary>
        /// <param name="services">The services collection containing the dependency injection services.</param>
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(AddHttpCorrelation) + " method overload with the " + nameof(HttpCorrelationInfoOptions) + " instead")]
        public static IServiceCollection AddHttpCorrelation(
            this IServiceCollection services,
            Action<CorrelationInfoOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the HTTP correlation services");

            services.AddHttpContextAccessor();
            services.AddCorrelation(
                serviceProvider => (HttpCorrelationInfoAccessor) serviceProvider.GetRequiredService<IHttpCorrelationInfoAccessor>(),
                configureOptions);
            services.AddSingleton<IHttpCorrelationInfoAccessor>(serviceProvider =>
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                return new HttpCorrelationInfoAccessor(httpContextAccessor);
            });
            services.AddSingleton(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<CorrelationInfoOptions>>();
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                var correlationInfoAccessor = serviceProvider.GetRequiredService<IHttpCorrelationInfoAccessor>();
                var logger = serviceProvider.GetService<ILogger<HttpCorrelation>>();

                return new HttpCorrelation(options, httpContextAccessor, correlationInfoAccessor, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds operation and transaction correlation to the application.
        /// </summary>
        /// <param name="services">The services collection containing the dependency injection services.</param>
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddHttpCorrelation(
            this IServiceCollection services,
            Action<HttpCorrelationInfoOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the HTTP correlation services");

            services.AddHttpContextAccessor();
            services.AddSingleton<IHttpCorrelationInfoAccessor>(serviceProvider =>
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                return new HttpCorrelationInfoAccessor(httpContextAccessor);
            });
            services.AddSingleton<ICorrelationInfoAccessor<CorrelationInfo>>(provider => provider.GetRequiredService<IHttpCorrelationInfoAccessor>());
            services.AddSingleton(provider => (ICorrelationInfoAccessor) provider.GetRequiredService<IHttpCorrelationInfoAccessor>());

            var options = new HttpCorrelationInfoOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(serviceProvider =>
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                var correlationInfoAccessor = serviceProvider.GetRequiredService<IHttpCorrelationInfoAccessor>();
                var logger = serviceProvider.GetService<ILogger<HttpCorrelation>>();
                
                return new HttpCorrelation(Options.Options.Create(options), httpContextAccessor, correlationInfoAccessor, logger);
            });

            if (options.Format is HttpCorrelationFormat.W3C)
            {
                services.AddApplicationInsightsTelemetry(opt =>
                {
                    opt.EnableRequestTrackingTelemetryModule = false;
                }); 
            }

            return services;
        }
    }
}