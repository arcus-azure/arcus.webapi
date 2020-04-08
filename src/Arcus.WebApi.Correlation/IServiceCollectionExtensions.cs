using System;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
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
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        [Obsolete("Correlation options is moved to 'Arcus.Observability.Correlation', use " + nameof(AddHttpCorrelation) + " instead")]
        public static IServiceCollection AddCorrelation(
            this IServiceCollection services,
            Action<CorrelationOptions> configureOptions = null)
        {
            if (configureOptions is null)
            {
                return AddHttpCorrelation(services, configureOptions: null);
            }

            Action<CorrelationInfoOptions> configureInfoOptions = infoOptions =>
            {
                var options = new CorrelationOptions
                {
                    Operation =
                    {
                        HeaderName = infoOptions.Operation.HeaderName,
                        GenerateId = infoOptions.Operation.GenerateId,
                        IncludeInResponse = infoOptions.Operation.IncludeInResponse
                    },
                    Transaction =
                    {
                        IncludeInResponse = infoOptions.Transaction.IncludeInResponse,
                        HeaderName = infoOptions.Transaction.HeaderName,
                        AllowInRequest = infoOptions.Transaction.AllowInRequest,
                        GenerateId = infoOptions.Transaction.GenerateId,
                        GenerateWhenNotSpecified = infoOptions.Transaction.GenerateWhenNotSpecified
                    }
                };

                configureOptions(options);
                infoOptions = options.ToCorrelationInfoOptions();
            };

            return AddHttpCorrelation(services, configureInfoOptions);
        }

        /// <summary>
        /// Adds operation and transaction correlation to the application.
        /// </summary>
        /// <param name="services">The services collection containing the dependency injection services.</param>
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        [Obsolete("Correlation is moved to 'Arcus.WebApi.Logging' package")]
        public static IServiceCollection AddHttpCorrelation(
            this IServiceCollection services, 
            Action<CorrelationInfoOptions> configureOptions = null)
        {
            Guard.NotNull(services, nameof(services));

            services.AddHttpContextAccessor();
            services.AddCorrelation(
                serviceProvider => new HttpCorrelationInfoAccessor(serviceProvider.GetRequiredService<IHttpContextAccessor>()), 
                configureOptions);

            return services;
        }
    }
}