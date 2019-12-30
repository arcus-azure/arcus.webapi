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
        /// Adds the correlation related services to the dependency injection system of the application.
        /// </summary>
        public static IServiceCollection AddCorrelation(this IServiceCollection services, Action<CorrelationOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services));

            AddCorrelation(services);

            services.Configure(configureOptions);

            return services;
        }

        /// <summary>
        /// Adds operation and transaction correlation to the application.
        /// </summary>
        public static IServiceCollection AddCorrelation(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            services.AddHttpContextAccessor();
            services.AddSingleton<HttpCorrelationInfo>();

            return services;
        }
    }
}