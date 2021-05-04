using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Serilog.Enrichers;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// Stub representation of the <see cref="IAppVersion"/>.
    /// </summary>
    /// <seealso cref="IAppVersion"/>
    public class StubAppVersion : IAppVersion
    {
        private readonly string _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubAppVersion"/> class.
        /// </summary>
        /// <param name="version">The current version of the application.</param>
        public StubAppVersion(string version)
        {
            _version = version;
        }

        /// <summary>
        /// Gets the current version of the application.
        /// </summary>
        public string GetVersion()
        {
            return _version;
        }
    }
}
