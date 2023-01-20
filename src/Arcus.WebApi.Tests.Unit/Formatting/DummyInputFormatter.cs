using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Arcus.WebApi.Tests.Unit.Formatting
{
    public class DummyInputFormatter : IInputFormatter
    {
        public bool CanRead(InputFormatterContext context)
        {
            throw new System.NotImplementedException();
        }

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
