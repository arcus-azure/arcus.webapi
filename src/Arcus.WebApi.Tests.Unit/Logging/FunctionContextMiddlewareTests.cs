using System.Threading.Tasks;
using Arcus.WebApi.Logging.AzureFunctions;
using Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class FunctionContextMiddlewareTests
    {
        [Fact]
        public async Task Invoke_WithFunctionContextAccessor_Succeeds()
        {
            // Arrange
            var accessor = new TestFunctionContextAccessor();
            var context = TestFunctionContext.Create();
            var middleware = new FunctionContextMiddleware(accessor);

            // Act
            await middleware.Invoke(context, ctx => Task.CompletedTask);

            // Assert
            Assert.Same(context, accessor.FunctionContext);
        }
    }
}
