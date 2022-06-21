﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GuardNet;
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
            catch (Exception exception)
            {
                string categoryName = _getLoggingCategory() ?? String.Empty;
                ILogger logger = loggerFactory.CreateLogger(categoryName) ?? NullLogger.Instance;
                LogException(logger, exception);

                WriteFailureToResponse(exception, context);
            }
        }

        /// <summary>
        /// Logs the caught <paramref name="exception"/> before the response it sent back.
        /// </summary>
        /// <param name="logger">The instance to track the caught <paramref name="exception"/>.</param>
        /// <param name="exception">The caught exception during the application pipeline.</param>
        protected virtual void LogException(ILogger logger, Exception exception)
        {
            logger.LogCritical(exception, exception.Message);
        }

        /// <summary>
        /// Determine the HTTP status code based on the caught exception.
        /// </summary>
        /// <param name="exception">The caught exception during the application pipeline.</param>
        /// <param name="context">The context instance for the current HTTP request.</param>
        /// <returns>An HTTP status code that represents the <paramref name="exception"/>.</returns>
        protected virtual void WriteFailureToResponse(Exception exception, HttpContext context)
        {
#if NET6_0
            if (exception is Microsoft.AspNetCore.Http.BadHttpRequestException badRequestException)
#else
            if (exception is Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException badRequestException)
#endif
            {
                // Catching the `BadHttpRequestException` and using the `.StatusCode` property allows us to interact with the built-in ASP.NET components.
                // When the Kestrel maximum request body restriction is exceeded, for example, this kind of exception is thrown.

                context.Response.StatusCode = badRequestException.StatusCode;
            }

            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
        }
    }
}
