using System;
using Correlate;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arcus.WebApi.Correlation 
{
    /// <summary>
    /// Adds operation & transaction correlation to the application.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the correlation related services to the dependency injection system of the application.
        /// Get the <see cref="ICorrelationAccessor"/> to retrieve the correlation information.
        /// </summary>
        public static IServiceCollection AddCorrelation(this IServiceCollection services, Action<CorrelationOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services));

            AddCorrelation(services);

            services.Configure(configureOptions);

            return services;
        }

        /// <summary>
        /// Adds operation & transaction correlation to the application.
        /// </summary>
        public static IServiceCollection AddCorrelation(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            var accessor = new CorrelationAccessor(new CorrelationContextAccessor());
            services.AddSingleton<ICorrelationAccessor>(accessor);
            services.AddSingleton<ICorrelationContextAccessor>(accessor);
            services.TryAddSingleton<ICorrelationContextFactory, CorrelationFactory>();
            services.TryAddSingleton<ICorrelationIdFactory, GuidCorrelationIdFactory>();
            services.TryAddTransient<IAsyncCorrelationManager, CorrelationManager>();
            services.TryAddTransient<ICorrelationManager, CorrelationManager>();
            services.TryAddTransient<IActivityFactory, CorrelationManager>();

            // From Correlate library: for backward compat, remove in future.
            services.TryAddTransient<CorrelationManager>();

            return services;
        }
    }
}