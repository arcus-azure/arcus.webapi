using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Tests.Unit.Hosting;
using Xunit;
using static Arcus.WebApi.Tests.Unit.Logging.SaboteurController;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly TestApiServer _testServer = new TestApiServer();

#if NETCOREAPP2_2
        [Fact]
        public async Task HandlesBadRequestException_ResultsInExceptionStatusCode()
        {
            // Arrange
            using (HttpClient client = _testServer.CreateClient())
            {
                // Act
                HttpResponseMessage response = await client.GetAsync(RequestBodyTooLargeRoute);

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }
#endif
    }
}
