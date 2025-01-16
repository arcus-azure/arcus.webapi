using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceProvider"/> related to web API security.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// Gets an instance of the registered <see cref="ILogger{TCategoryName}"/> or provide the default <see cref="NullLogger{TCategoryName}.Instance"/>.
        /// </summary>
        /// <typeparam name="T">The type who's name is used for the logger category name.</typeparam>
        /// <param name="services">The services to retrieve the <see cref="ILogger{TCategoryName}"/> instance.</param>
        /// <returns>
        ///     Either the registered <see cref="ILogger{TCategoryName}"/> or the default <see cref="NullLogger{TCategoryName}.Instance"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static ILogger<T> GetLoggerOrDefault<T>(this IServiceProvider services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services), "Requires a services collection to retrieve a logger instance");
            }
            
            var loggerFactory = services.GetService<ILoggerFactory>();
            ILogger<T> logger = loggerFactory?.CreateLogger<T>();

            return logger ?? NullLogger<T>.Instance;
        }
    }
}
