using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Logging
{
    public class LogExceptionsFilter : ExceptionFilterAttribute
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogExceptionsFilter"/> class.
        /// </summary>
        public LogExceptionsFilter(ILogger logger)
        {
            _logger = logger;
        }

        public override void OnException(ExceptionContext context)
        {
            _logger.LogCritical(context.Exception, context.Exception.Message);

            context.HttpContext.Response.StatusCode = 500;
        }
    }
}
