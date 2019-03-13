using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Interfaces;
using Arcus.WebApi.Security.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    public class SharedAccessKeyAuthenticationAttributeTests
    {
        [Fact]
        public async Task OnActionExecutionAsync_MatchRequestHeaderWithSecret_ShouldPassThru()
        {
            // Arrange
            var secret = Guid.NewGuid().ToString();
            var name = Guid.NewGuid().ToString();
            var sut = new SharedAccessKeyAuthenticationAttribute(headerName: name, secretName: name);

            var requestServices = new ServiceContainer();
            requestServices.AddService(typeof(ISecretProvider), new StubSecretProvider(secret));

            // Act
            ActionExecutingContext context =
                await ExerciseAuthenticationFilter(
                    sut,
                    new Dictionary<string, StringValues>
                    {
                        [name] = secret
                    },
                    requestServices);

            // Assert
            Assert.Null(context.Result);
        }
        
        [Theory]
        [InlineData("not-matching-header", "secret")]
        [InlineData("header", "not-matching-secret")]
        public async Task OnActionExecutingAsync_UnmatchedRequestHeader_ShouldRespondUnauthorized(
            string name,
            string secretValue)
        {
            // Arrange
            var sut = new SharedAccessKeyAuthenticationAttribute(headerName: name, secretName: name);

            var requestServices = new ServiceContainer();
            requestServices.AddService(typeof(ISecretProvider), new StubSecretProvider("secret"));

            // Act
            ActionExecutingContext context = await ExerciseAuthenticationFilter(
                sut,
                new Dictionary<string, StringValues>
                {
                    ["header"] = secretValue
                },
                requestServices);

            // Assert
            Assert.IsType<UnauthorizedResult>(context.Result);
        }

        private static async Task<ActionExecutingContext> ExerciseAuthenticationFilter(
            SharedAccessKeyAuthenticationAttribute sut,
            Dictionary<string, StringValues> requestHeaders,
            IServiceContainer requestServices)
        {
            var context = new ActionExecutingContext(
                new ActionContext(
                    new StubHttpContext(
                        requestHeaders,
                        requestServices),
                    new RouteData(),
                    new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                controller: null);

            await sut.OnActionExecutionAsync(
                context,
                () => Task.FromResult(
                    new ActionExecutedContext(
                        new ActionContext(),
                        new List<IFilterMetadata>(),
                        controller: null)));

            return context;
        }
    }
}
