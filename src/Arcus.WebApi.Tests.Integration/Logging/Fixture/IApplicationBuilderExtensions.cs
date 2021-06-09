using System;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseTraceIdentifier(this IApplicationBuilder app, Action<TraceIdentifierOptions> configureOptions = null)
        {
            var options = new TraceIdentifierOptions();
            configureOptions?.Invoke(options);

            return app.UseMiddleware<TraceIdentifierMiddleware>(options);
        }
    }
}
