﻿using System;
using System.Collections.Generic;
using Arcus.WebApi.Security.Authorization.Jwt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Arcus.WebApi.Security.Authorization
{
    /// <summary>
    /// Represents the authorization options to configure the <see cref="JwtTokenAuthorizationFilter"/>.
    /// </summary>
    public class JwtTokenAuthorizationOptions
    {
        /// <summary>
        /// Gets the default header name to look for the JWT token.
        /// </summary>
        public const string DefaultHeaderName = "x-identity-token";

        private string _headerName;
        private IJwtTokenReader _jwtTokenReader;
        private Func<IServiceProvider, IJwtTokenReader> _createJwtTokenReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationOptions"/> class.
        /// </summary>
        public JwtTokenAuthorizationOptions()
            : this(new JwtTokenReader(new TokenValidationParameters()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationOptions"/> class.
        /// </summary>
        /// <param name="reader">The JWT reader to verify the token from the HTTP request header.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reader"/> is <c>null</c>.</exception>
        public JwtTokenAuthorizationOptions(IJwtTokenReader reader)
            : this(reader, DefaultHeaderName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationOptions"/> class.
        /// </summary>
        /// <param name="claimCheck">Custom claims key-value pair to validate against.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public JwtTokenAuthorizationOptions(IDictionary<string, string> claimCheck)
            : this(new JwtTokenReader(new TokenValidationParameters(), claimCheck))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationOptions"/> class.
        /// </summary>
        /// <param name="reader">The JWT reader to verify the token from the HTTP request header.</param>
        /// <param name="headerName">The name of the header where the JWT token is expected.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reader"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> is blank.</exception>
        public JwtTokenAuthorizationOptions(IJwtTokenReader reader, string headerName)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader), $"Requires a valid {nameof(IJwtTokenReader)} to verify the JWT token");
            }

            if (string.IsNullOrWhiteSpace(headerName))
            {
                throw new ArgumentException("Requires a non-blank request header name to look for the JWT token", nameof(headerName));
            }

            JwtTokenReader = reader;
            HeaderName = headerName;
        }

        /// <summary>
        /// Gets or sets the header name where the JWT token is expected in the HTTP request.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string HeaderName
        {
            get => _headerName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank request header name to look for the JWT token", nameof(value));
                }

                _headerName = value;
            }
        }

        /// <summary>
        /// Gets or sets the JSON web token reader to verify the token from the HTTP request header.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="value"/> is <c>null</c>.</exception>
        public IJwtTokenReader JwtTokenReader
        {
            get => _jwtTokenReader;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value), $"Requires a valid {nameof(IJwtTokenReader)} to verify the JWT token");
                }

                _jwtTokenReader = value;
                _createJwtTokenReader = null;
            }
        }
        
        /// <summary>
        /// Gets or sets the flag indicating whether or not the JWT authorization should emit security events when authorizing the request.
        /// </summary>
        public bool EmitSecurityEvents { get; set; }

        /// <summary>
        /// Use the provided <typeparamref name="TImplementation"/> instance to verify the token from the HTTP request header,
        /// injected via registered services in the dependency container.
        /// </summary>
        /// <typeparam name="TImplementation">The type of the <see cref="IJwtTokenReader"/> implementation.</typeparam>
        public void AddJwtTokenReader<TImplementation>() where TImplementation : IJwtTokenReader
        {
            _jwtTokenReader = null;
            _createJwtTokenReader = serviceProvider => ActivatorUtilities.GetServiceOrCreateInstance<TImplementation>(serviceProvider);
        }

        /// <summary>
        /// Use the provided <paramref name="createReader"/> implementation function to create an <see cref="IJwtTokenReader"/> instance to verify the token in the HTTP request header.
        /// </summary>
        /// <param name="createReader">The implementation function to create an <see cref="IJwtTokenReader"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="createReader"/> is <c>null</c>.</exception>
        public void AddJwtTokenReader(Func<IServiceProvider, IJwtTokenReader> createReader)
        {
            _jwtTokenReader = null;
            _createJwtTokenReader = createReader ?? throw new ArgumentNullException(nameof(createReader), $"Requires an implementation function to create an {nameof(IJwtTokenReader)} instance");
        }

        /// <summary>
        /// Gets the <see cref="JwtTokenReader"/> or create one with the previously provided implementation function.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        internal IJwtTokenReader GetOrCreateJwtTokenReader(IServiceProvider serviceProvider)
        {
            if (_createJwtTokenReader != null)
            {
                try
                {
                    if (serviceProvider is null)
                    {
                        throw new ArgumentNullException(nameof(serviceProvider), $"Requires a collection of services to create an {nameof(IJwtTokenReader)} instance");
                    }

                    return _createJwtTokenReader(serviceProvider);
                }
                catch (Exception exception)
                {
                    var logger = serviceProvider.GetService<ILogger<JwtTokenAuthorizationOptions>>();
                    logger.LogError(exception, "Cannot create an instance of the {Type}", nameof(IJwtTokenReader));

                    return null;
                }
            }

            return _jwtTokenReader;
        }
    }
}