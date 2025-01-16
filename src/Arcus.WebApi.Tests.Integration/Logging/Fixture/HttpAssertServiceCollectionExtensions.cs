using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to register more easily <see cref="HttpAssert"/> instances.
    /// </summary>
    public static class HttpAssertServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="HttpAssert"/> instance with an unique <paramref name="name"/>
        /// which can be run as a 'backdoor' assertion on any of the Web API application components.
        /// </summary>
        /// <param name="services">The application services to add the HTTP assertion to.</param>
        /// <param name="name">The unique name to register the HTTP assertion under.</param>
        /// <param name="assertion">The assertion function to verify the current available HTTP context.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="assertion"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> is blank.</exception>
        public static IServiceCollection AddHttpAssert(this IServiceCollection services, string name, Action<HttpContext> assertion)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services), "Requires a set of services to add the HTTP assertion to");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Requires a non-blank name to register the HTTP assertion", nameof(name));
            }

            if (assertion is null)
            {
                throw new ArgumentNullException(nameof(assertion), "Requires an assertion function to verify the currently available HTTP context");
            }

            services.TryAddSingleton<HttpAssertProvider>();
            return services.AddSingleton(Tuple.Create(name, HttpAssert.Create(assertion)));
        }
    }
}