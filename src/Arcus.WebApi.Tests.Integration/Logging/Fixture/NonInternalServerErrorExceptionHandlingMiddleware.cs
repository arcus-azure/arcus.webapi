using System;
using System.Net;
using Arcus.WebApi.Logging;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// Represents a custom <see cref="ExceptionHandlingMiddleware"/> component to test overridable functionality.
    /// </summary>
    public class NonInternalServerErrorExceptionHandlingMiddleware : ExceptionHandlingMiddleware
    {
        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="NonInternalServerErrorExceptionHandlingMiddleware" /> class.
        /// </summary>
        public NonInternalServerErrorExceptionHandlingMiddleware(
            RequestDelegate next) : base(next)
        {
        }

        protected override void LogException(ILogger logger, Exception exception)
        {
            logger.LogCritical(exception, "Testing!");
        }

        protected override HttpStatusCode DetermineResponseStatusCode(Exception exception)
        {
            return BogusGenerator.PickRandomWithout(HttpStatusCode.InternalServerError);
        }
    }
}
