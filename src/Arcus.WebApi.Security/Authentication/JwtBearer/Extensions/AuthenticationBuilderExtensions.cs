using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="AuthenticationBuilder"/> to add JWT Bearer specific functionality.
    /// </summary>
    public static class AuthenticationBuilderExtensions
    {
        /// <summary>
        /// Enables JWT-bearer authentication using the default scheme <see cref="JwtBearerDefaults.AuthenticationScheme"/>/
        /// JWT bearer authentication performs authentication by extracting and validating a JWT token from the Authorization request header.
        /// </summary>
        /// <param name="builder">The builder instance to add the JWT bearer authentication to.</param>
        /// <param name="configureOptions">The function to configure the JWT bearer options that affects the authentication process.</param>
        /// <returns>A reference to builder after the operation has completed.</returns>
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, Action<JwtBearerOptions, IServiceProvider> configureOptions)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires an authentication builder instance to add the JWT Bearer authentication");
            }

            if (configureOptions != null)
            {
                builder.Services.AddOptions();
                builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(serviceProvider =>
                {
                    return new ConfigureNamedOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => configureOptions(options, serviceProvider));
                });
            }

            return builder.AddJwtBearer(configureOptions: (Action<JwtBearerOptions>) null);
        }
    }
}
