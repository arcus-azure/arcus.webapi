using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.AzureFunctions;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using GuardNet;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extensions on the <see cref="IFunctionsWorkerApplicationBuilder"/> related to HTTP correlation.
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
            Guard.NotNull(builder, nameof(builder), "Requires a function worker builder instance to add the function context middleware");

            builder.Services.AddScoped<IFunctionContextAccessor, DefaultFunctionContextAccessor>();
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
        {
            Guard.NotNull(builder, nameof(builder), "Requires a function worker builder instance to add the HTTP correlation middleware");

            return UseHttpCorrelation(builder, options => { });
        }

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
            Guard.NotNull(builder, nameof(builder), "Requires a function worker builder instance to add the HTTP correlation middleware");

            builder.Services.AddScoped<ICorrelationInfoAccessor<CorrelationInfo>>(provider => provider.GetRequiredService<IHttpCorrelationInfoAccessor>());
            builder.Services.AddScoped(provider => (ICorrelationInfoAccessor) provider.GetRequiredService<IHttpCorrelationInfoAccessor>());
            builder.Services.AddScoped<IHttpCorrelationInfoAccessor, AzureFunctionsHttpCorrelationInfoAccessor>();

            builder.Services.AddScoped(provider =>
            {
                var options = new HttpCorrelationInfoOptions();
                configureOptions?.Invoke(options);

                var correlationAccessor = provider.GetRequiredService<IHttpCorrelationInfoAccessor>();
                var logger = provider.GetService<ILogger<AzureFunctionsHttpCorrelation>>();

                return new AzureFunctionsHttpCorrelation(options, correlationAccessor, logger);
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
            Guard.NotNull(builder, nameof(builder), "Requires a function worker builder instance to add the HTTP exception handling middleware");
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
            Guard.NotNull(builder, nameof(builder), "Requires a function worker builder instance to add the HTTP exception handling middleware");
            return builder.UseMiddleware<TMiddleware>();
        }
    }
}
