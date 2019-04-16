using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Logging
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ILoggerFactory loggerFactory)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                HandleException(context, ex, loggerFactory);
            }
        }

        private static void HandleException(HttpContext context, Exception ex, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("");
            logger.LogCritical(ex, ex.Message);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
}
