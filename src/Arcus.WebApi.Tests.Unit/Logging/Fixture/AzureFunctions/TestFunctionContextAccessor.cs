using Arcus.WebApi.Logging.AzureFunctions;
using Microsoft.Azure.Functions.Worker;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class TestFunctionContextAccessor : IFunctionContextAccessor
    {
        public FunctionContext FunctionContext { get; set; }
    }
}
