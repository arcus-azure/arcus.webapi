using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    public class NullCorrelationInfoAccessor : IHttpCorrelationInfoAccessor
    {
        public CorrelationInfo GetCorrelationInfo()
        {
            return null;
        }

        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
        }
    }
}
