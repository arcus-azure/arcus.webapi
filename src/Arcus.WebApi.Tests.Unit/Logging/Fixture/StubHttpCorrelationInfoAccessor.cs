using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture
{
    /// <summary>
    /// Represents an <see cref="IHttpCorrelationInfoAccessor"/> that retrieves the HTTP correlation internally.
    /// </summary>
    public class StubHttpCorrelationInfoAccessor : IHttpCorrelationInfoAccessor
    {
        private CorrelationInfo _correlation;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubHttpCorrelationInfoAccessor" /> class.
        /// </summary>
        public StubHttpCorrelationInfoAccessor()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StubHttpCorrelationInfoAccessor" /> class.
        /// </summary>
        /// <param name="correlation">The currently available HTTP correlation instance.</param>
        public StubHttpCorrelationInfoAccessor(CorrelationInfo correlation)
        {
            _correlation = correlation;
        }

        public CorrelationInfo GetCorrelationInfo()
        {
            return _correlation;
        }

        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
            _correlation = correlationInfo;
        }
    }
}
