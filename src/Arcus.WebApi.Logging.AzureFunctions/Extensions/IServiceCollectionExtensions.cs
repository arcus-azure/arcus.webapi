using System;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

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
        /// <param name="configureOptions">The function to configure additional options how the correlation works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IServiceCollection AddHttpCorrelation(this IFunctionsHostBuilder builder, Action<CorrelationInfoOptions> configureOptions = null)
        {
            Guard.NotNull(builder, nameof(builder));

            return builder.Services.AddHttpCorrelation(configureOptions);
        }
    }
}