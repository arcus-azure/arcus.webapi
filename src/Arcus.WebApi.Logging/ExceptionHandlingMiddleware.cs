using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GuardNet;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Exception handling middleware that handles exceptions thrown further up the ASP.NET Core request pipeline.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Func<string> _getLoggingCategory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next <see cref="RequestDelegate"/> in the ASP.NET Core request pipeline.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="next"/> is <c>null</c>.</exception>
        public ExceptionHandlingMiddleware(RequestDelegate next)
            : this(next, categoryName: String.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next <see cref="RequestDelegate"/> in the ASP.NET Core request pipeline.</param>
        /// <param name="categoryName">The category-name for messages produced by the logger.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="next"/> is <c>null</c>.</exception>
        public ExceptionHandlingMiddleware(RequestDelegate next, string categoryName)
            : this(next, getLoggingCategory: () => categoryName)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next <see cref="RequestDelegate"/> in the ASP.NET Core request pipeline.</param>
        /// <param name="getLoggingCategory">The function that returns the category-name that must be used by the logger when writing log messages.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="next"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">When the <paramref name="getLoggingCategory"/> is <c>null</c>.</exception>
        public ExceptionHandlingMiddleware(RequestDelegate next, Func<string> getLoggingCategory)
        {
            Guard.NotNull(next, nameof(next), "The next request delegate in the application request pipeline cannot be null");
            Guard.NotNull(getLoggingCategory, nameof(getLoggingCategory), "The retrieval of the logging category function cannot be null");

            _next = next;
            _getLoggingCategory = getLoggingCategory;
        }

        /// <summary>
        /// Invoke the middleware to handle exceptions thrown further up the request pipeline.
        /// </summary>
        /// <param name="context">The context for the current HTTP request.</param>
        /// <param name="loggerFactory">The factory instance to create <see cref="ILogger"/> instances.</param>
        public async Task Invoke(HttpContext context, ILoggerFactory loggerFactory)
        {
            try
            {
                await _next(context);
            }
            catch (BadHttpRequestException ex)
            {
                LogException(loggerFactory, ex);
                context.Response.StatusCode = ex.StatusCode;
            }
            catch (Exception ex)
            {
                LogException(loggerFactory, ex);
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
            }
        }

        private void LogException(ILoggerFactory loggerFactory, Exception ex)
        {
            string categoryName = _getLoggingCategory() ?? String.Empty;
            var logger = loggerFactory.CreateLogger(categoryName) ?? NullLogger.Instance;

            logger.LogCritical(ex, ex.Message);
        }
    }
}
