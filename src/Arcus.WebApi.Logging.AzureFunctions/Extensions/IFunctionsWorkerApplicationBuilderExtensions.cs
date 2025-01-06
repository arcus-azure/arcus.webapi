using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.AzureFunctions;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extensions on the <see cref="IFunctionsWorkerApplicationBuilder"/> related to logging.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IFunctionsWorkerApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds a middleware component that exposes the <see cref="FunctionContext"/> in a scoped service <see cref="IFunctionContextAccessor"/>.
        /// </summary>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseFunctionContext(
            this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a function worker builder instance to add the function context middleware");
            }

            builder.Services.AddSingleton<IFunctionContextAccessor, DefaultFunctionContextAccessor>();
            builder.UseMiddleware<FunctionContextMiddleware>();

            return builder;
        }

        /// <summary>
        /// Adds a middleware component that exposes the <see cref="FunctionContext"/> in a scoped service <see cref="IFunctionContextAccessor"/>.
        /// </summary>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseHttpCorrelation(
            this IFunctionsWorkerApplicationBuilder builder)
            => UseHttpCorrelation(builder, options => { });

        /// <summary>
        /// Adds a middleware component that exposes the <see cref="FunctionContext"/> in a scoped service <see cref="IFunctionContextAccessor"/>.
        /// </summary>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <param name="configureOptions">The function to configure the additional HTTP correlation options that alters the behavior of the HTTP correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseHttpCorrelation(
            this IFunctionsWorkerApplicationBuilder builder,
            Action<HttpCorrelationInfoOptions> configureOptions)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a function worker builder instance to add the HTTP correlation middleware");
            }

            builder.Services.AddApplicationInsightsTelemetryWorkerService();
            builder.Services.ConfigureFunctionsApplicationInsights();

            builder.Services.AddSingleton<ICorrelationInfoAccessor<CorrelationInfo>>(provider => provider.GetRequiredService<IHttpCorrelationInfoAccessor>());
            builder.Services.AddSingleton(provider => (ICorrelationInfoAccessor) provider.GetRequiredService<IHttpCorrelationInfoAccessor>());
            builder.Services.AddSingleton<IHttpCorrelationInfoAccessor, AzureFunctionsHttpCorrelationInfoAccessor>();

            builder.Services.AddSingleton(provider =>
            {
                var options = new HttpCorrelationInfoOptions();
                configureOptions?.Invoke(options);

                var correlationAccessor = provider.GetRequiredService<IHttpCorrelationInfoAccessor>();
                var logger = provider.GetService<ILogger<AzureFunctionsHttpCorrelation>>();

                switch (options.Format)
                {
                    case HttpCorrelationFormat.W3C:
                        var client = provider.GetRequiredService<TelemetryClient>();
                        return new AzureFunctionsHttpCorrelation(client, options, correlationAccessor, logger);
                    case HttpCorrelationFormat.Hierarchical:
                        return new AzureFunctionsHttpCorrelation(options, correlationAccessor, logger);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(options), options.Format, "Unknown HTTP correlation format");
                }
            });

            builder.UseMiddleware<AzureFunctionsCorrelationMiddleware>();
            return builder;
        }

        /// <summary>
        /// Adds a middleware component that catches unhandled exceptions and provides a general failure for the consumer.
        /// </summary>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseExceptionHandling(this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a function worker builder instance to add the HTTP exception handling middleware");
            }

            return builder.UseMiddleware<AzureFunctionsExceptionHandlingMiddleware>();
        }

        /// <summary>
        /// Adds a middleware component that catches unhandled exceptions and provides a general failure for the consumer.
        /// </summary>
        /// <typeparam name="TMiddleware">The custom type that inherits the <see cref="AzureFunctionsExceptionHandlingMiddleware"/> type.</typeparam>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseExceptionHandling<TMiddleware>(this IFunctionsWorkerApplicationBuilder builder)
            where TMiddleware : AzureFunctionsExceptionHandlingMiddleware
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a function worker builder instance to add the HTTP exception handling middleware");
            }

            return builder.UseMiddleware<TMiddleware>();
        }

        /// <summary>
        /// Adds a middleware component that tracks the HTTP request.
        /// </summary>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseRequestTracking(
            this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a function worker builder instance to add the HTTP request tracking middleware");
            }

            return UseRequestTracking(builder, configureOptions: null);
        }

        /// <summary>
        /// Adds a middleware component that tracks the HTTP request.
        /// </summary>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <param name="configureOptions">The function to configure the HTTP request tracking options that influence the way the HTTP request is tracked.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseRequestTracking(
            this IFunctionsWorkerApplicationBuilder builder,
            Action<RequestTrackingOptions> configureOptions)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a function worker builder instance to add the HTTP request tracking middleware");
            }

            return UseRequestTracking<AzureFunctionsRequestTrackingMiddleware>(builder, configureOptions);
        }

        /// <summary>
        /// Adds a middleware component that tracks the HTTP request.
        /// </summary>
        /// <typeparam name="TMiddleware">The custom type that inherits from the <see cref="AzureFunctionsRequestTrackingMiddleware"/> type.</typeparam>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseRequestTracking<TMiddleware>(
            this IFunctionsWorkerApplicationBuilder builder)
            where TMiddleware : AzureFunctionsRequestTrackingMiddleware
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a function worker builder instance to add the HTTP request tracking middleware");
            }

            return UseRequestTracking<TMiddleware>(builder, configureOptions: null);
        }

        /// <summary>
        /// Adds a middleware component that tracks the HTTP request.
        /// </summary>
        /// <typeparam name="TMiddleware">The custom type that inherits from the <see cref="AzureFunctionsRequestTrackingMiddleware"/> type.</typeparam>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <param name="configureOptions">The function to configure the HTTP request tracking options that influence the way the HTTP request is tracked.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseRequestTracking<TMiddleware>(
            this IFunctionsWorkerApplicationBuilder builder,
            Action<RequestTrackingOptions> configureOptions)
            where TMiddleware : AzureFunctionsRequestTrackingMiddleware
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a function worker builder instance to add the HTTP request tracking middleware");
            }

            var options = new RequestTrackingOptions();
            configureOptions?.Invoke(options);
            builder.Services.AddSingleton(options);
            
            return builder.UseMiddleware<TMiddleware>();
        }
    }
}
