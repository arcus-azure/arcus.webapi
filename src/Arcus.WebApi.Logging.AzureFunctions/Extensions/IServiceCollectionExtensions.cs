using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
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
        /// <param name="builder">The functions host builder containing the dependency injection services.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IServiceCollection AddHttpCorrelation(this IFunctionsHostBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a function host builder instance to add the HTTP correlation services");

            return AddHttpCorrelation(builder, (HttpCorrelationInfoOptions options) => { });
        }

        /// <summary>
        /// Adds operation and transaction correlation to the application.
        /// </summary>
        /// <param name="builder">The functions host builder containing the dependency injection services.</param>
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(AddHttpCorrelation) + " method overload with the " + nameof(HttpCorrelationInfoOptions) + " instead")]
        public static IServiceCollection AddHttpCorrelation(this IFunctionsHostBuilder builder, Action<CorrelationInfoOptions> configureOptions)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a functions host builder instance to add the HTTP correlation services");

            IServiceCollection services = builder.Services;
            services.AddHttpContextAccessor();
            services.AddCorrelation(
                serviceProvider => (HttpCorrelationInfoAccessor) serviceProvider.GetRequiredService<IHttpCorrelationInfoAccessor>(),
                configureOptions);
            services.AddScoped<IHttpCorrelationInfoAccessor>(serviceProvider =>
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                return new HttpCorrelationInfoAccessor(httpContextAccessor);
            });
            services.AddScoped(serviceProvider =>
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
        /// <param name="builder">The functions host builder containing the dependency injection services.</param>
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IServiceCollection AddHttpCorrelation(this IFunctionsHostBuilder builder, Action<HttpCorrelationInfoOptions> configureOptions)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a function host builder instance to add the HTTP correlation services");

            IServiceCollection services = builder.Services;

            var options = new HttpCorrelationInfoOptions();
            configureOptions?.Invoke(options);

            if (options.Format is HttpCorrelationFormat.W3C)
            {
                services.AddSingleton<IHttpCorrelationInfoAccessor, ActivityCorrelationInfoAccessor>();
                services.AddSingleton(serviceProvider =>
                {
                    var correlationInfoAccessor = serviceProvider.GetRequiredService<IHttpCorrelationInfoAccessor>();
                    var logger = serviceProvider.GetService<ILogger<AzureFunctionsInProcessHttpCorrelation>>();
                    return new AzureFunctionsInProcessHttpCorrelation(options, correlationInfoAccessor, logger);
                });

                services.AddApplicationInsightsTelemetryWorkerService();
            }

            if (options.Format is HttpCorrelationFormat.Hierarchical)
            {
                services.AddHttpContextAccessor();
                services.AddSingleton<IHttpCorrelationInfoAccessor>(serviceProvider =>
                {
                    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                    return new HttpCorrelationInfoAccessor(httpContextAccessor);
                });
                services.AddSingleton(serviceProvider =>
                {
                    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                    var correlationInfoAccessor = serviceProvider.GetRequiredService<IHttpCorrelationInfoAccessor>();
                    var logger = serviceProvider.GetService<ILogger<HttpCorrelation>>();
                
                    return new HttpCorrelation(Options.Options.Create(options), httpContextAccessor, correlationInfoAccessor, logger);
                });
            }

            services.AddSingleton<ICorrelationInfoAccessor<CorrelationInfo>>(provider => provider.GetRequiredService<IHttpCorrelationInfoAccessor>());
            services.AddSingleton(provider => (ICorrelationInfoAccessor) provider.GetRequiredService<IHttpCorrelationInfoAccessor>());

            return services;
        }
    }
}