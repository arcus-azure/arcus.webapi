using System;
using Arcus.WebApi.Hosting.AzureFunctions.Formatting;
using GuardNet;
using Microsoft.Azure.Functions.Worker;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extensions on the <see cref="IFunctionsWorkerApplicationBuilder"/> related to hosting.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IFunctionsWorkerApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the middleware component to only allow JSON formatted HTTP requests.
        /// </summary>
        /// <param name="builder">The Azure Functions application builder instance to build up the application and middleware pipeline.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsWorkerApplicationBuilder UseOnlyJsonFormatting(
            this IFunctionsWorkerApplicationBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a function worker builder instance to add the JSON formatting middleware");

            builder.UseMiddleware<AzureFunctionsJsonFormattingMiddleware>();
            return builder;
        }
    }
}
