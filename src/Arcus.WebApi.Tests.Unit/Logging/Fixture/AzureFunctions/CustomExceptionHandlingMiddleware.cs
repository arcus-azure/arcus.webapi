﻿using System;
using System.Net;
using Arcus.WebApi.Logging.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class CustomExceptionHandlingMiddleware : AzureFunctionsExceptionHandlingMiddleware
    {
        private readonly HttpStatusCode _statusCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomExceptionHandlingMiddleware" /> class.
        /// </summary>
        public CustomExceptionHandlingMiddleware()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomExceptionHandlingMiddleware" /> class.
        /// </summary>
        public CustomExceptionHandlingMiddleware(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override void LogException(ILogger logger, Exception exception)
        {
            logger.LogCritical(exception, "Custom exception handling message");
        }

        protected override HttpResponseData CreateFailureResponse(Exception exception, HttpStatusCode defaultFailureStatusCode, HttpRequestData request)
        {
            return request.CreateResponse(_statusCode);
        }
    }
}
