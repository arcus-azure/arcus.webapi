using System;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.WebApi.Correlation 
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
        public static IServiceCollection AddCorrelation(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            return AddCorrelation(services, configureOptions: null);
        }

        /// <summary>
        /// Adds operation and transaction correlation to the application.
        /// </summary>
        /// <param name="services">The services collection containing the dependency injection services.</param>
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        public static IServiceCollection AddCorrelation(
            this IServiceCollection services, 
            Action<CorrelationOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services));

            services.AddHttpContextAccessor();
            services.AddSingleton<HttpCorrelationInfo>();

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }
    }
}