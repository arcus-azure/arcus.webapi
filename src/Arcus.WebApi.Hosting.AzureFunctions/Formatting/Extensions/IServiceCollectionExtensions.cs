using System;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> via the <see cref="IFunctionsWorkerApplicationBuilder"/> for easier formatting configuration.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Configure the JSON formatting on the <see cref="JsonObjectSerializer"/> which will be added to the application services.
        /// </summary>
        /// <param name="builder">The Azure Functions application builder with the registered application services.</param>
        /// <param name="configureOptions">The function to configure the JSON serialization options for the <see cref="JsonObjectSerializer"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="configureOptions"/> is <c>null</c>.</exception>
        public static IServiceCollection ConfigureJsonFormatting(
            this IFunctionsWorkerApplicationBuilder builder,
            Action<JsonSerializerOptions> configureOptions)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires an Azure Functions application builder instance to add the JSON serializer to the application services");
            }

            if (configureOptions is null)
            {
                throw new ArgumentNullException(nameof(configureOptions), "Requires a function to configure the JSON serialization options to add the JSON serializer to the application services");
            }

            var options = new JsonSerializerOptions();
            configureOptions(options);

            return builder.Services.AddSingleton(new JsonObjectSerializer(options));
        }
    }
}
