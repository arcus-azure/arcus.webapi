using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    public class NullCorrelationInfoAccessor : ICorrelationInfoAccessor
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
