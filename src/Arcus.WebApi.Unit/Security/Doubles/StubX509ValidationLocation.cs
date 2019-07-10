using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.Interfaces;

namespace Arcus.WebApi.Unit.Security.Doubles
{
    /// <summary>
    /// Stubbed implementation of the <see cref="IX509ValidationLocation"/>.
    /// </summary>
    public class StubX509ValidationLocation : IX509ValidationLocation
    {
        private readonly string _stubbedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubX509ValidationLocation"/> class.
        /// </summary>
        /// <param name="stubbedValue">The value to stub as result for accessing the certificate value.</param>
        public StubX509ValidationLocation(string stubbedValue)
        {
            _stubbedValue = stubbedValue;
        }

        /// <summary>
        /// Gets the expected value in a <see cref="System.Security.Cryptography.X509Certificates.X509Certificate2"/> for an <paramref name="configurationKey"/> using the specified <paramref name="services"/>.
        /// </summary>
        /// <param name="configurationKey">The configured key for which the expected certificate value is registered.</param>
        /// <param name="services">The services collections of the HTTP request pipeline to retrieve registered instances.</param>
        public Task<string> GetExpectedCertificateValueForConfiguredKeyAsync(string configurationKey, IServiceProvider services)
        {
            return Task.FromResult(_stubbedValue);
        }
    }
}
