using System;
using Arcus.WebApi.Security.Authentication.Certificates;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to more easily register the <see cref="CertificateAuthenticationValidator"/>.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds certificate authentication validation to the application services.
        /// This is required if the certificate authentication is set up in the application via attributes instead of global authentication filters.
        /// </summary>
        /// <remarks>
        ///     When the Arcus secret store is used during the <paramref name="configureAuthentication"/> to register the certificate locations,
        ///     make sure that the secret store is registered to avoid runtime failures.
        ///     For more information on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The application services to register the certificate authentication validator.</param>
        /// <param name="configureAuthentication">
        ///     The function to configure what information of the certificate should be used to validate the certificate during authentication.
        ///     Requires at least a single configuration location to validate the certificate.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="configureAuthentication"/> is <c>null</c></exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when no authentication location on the certificate was configured during the <paramref name="configureAuthentication"/> function.
        /// </exception>
        public static IServiceCollection AddCertificateAuthenticationValidation(
            this IServiceCollection services,
            Action<CertificateAuthenticationConfigBuilder> configureAuthentication)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services), "Requires a set of application services to register the certificate authentication validator");
            }
            if (configureAuthentication is null)
            {
                throw new ArgumentNullException(nameof(configureAuthentication), "Requires a function to configure the certificate validation locations");
            }

            var builder = new CertificateAuthenticationConfigBuilder();
            configureAuthentication(builder);
            CertificateAuthenticationConfig config = builder.Build();

            return services.AddSingleton(new CertificateAuthenticationValidator(config));
        }
    }
}
