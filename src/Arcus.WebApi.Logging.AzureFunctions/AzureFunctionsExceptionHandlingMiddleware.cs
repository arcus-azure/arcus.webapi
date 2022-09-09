using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Logging.AzureFunctions
{
    /// <summary>
    /// Exception handling middleware that handles exceptions in (isolated) Azure Functions HTTP triggers thrown further up the request pipeline.
    /// </summary>
    public class AzureFunctionsExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
    {
        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext" /> for the current invocation.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A <see cref="Task" /> that represents the asynchronous invocation.</returns>
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                ILogger logger = context.GetLogger<AzureFunctionsExceptionHandlingMiddleware>();
                LogException(logger, exception);

                HttpRequestData request = await DetermineHttpRequestAsync(context);
                HttpResponseData response = CreateFailureResponse(exception, HttpStatusCode.InternalServerError, request);
                context.GetInvocationResult().Value = response;
            }
        }

        private static async Task<HttpRequestData> DetermineHttpRequestAsync(FunctionContext context)
        {
            HttpRequestData request = await context.GetHttpRequestDataAsync();
            if (request is null)
            {
                throw new InvalidOperationException(
                    "Cannot determine HTTP request in HTTP exception handling middleware, this probably means that their is no HTTP trigger function triggering this invocation."
                    + "Please only use the HTTP exception handling in HTTP scenarios");
            }

            return request;
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
        /// Create a HTTP response based on the caught exception.
        /// </summary>
        /// <param name="exception">The caught exception during the application pipeline.</param>
        /// <param name="defaultFailureStatusCode">The default HTTP status code for the failure that was determined by the caught <paramref name="exception"/>.</param>
        /// <param name="request">The current HTTP request that was caught.</param>
        /// <returns>An HTTP status code that represents the <paramref name="exception"/>.</returns>
        protected virtual HttpResponseData CreateFailureResponse(Exception exception, HttpStatusCode defaultFailureStatusCode, HttpRequestData request)
        {
            HttpResponseData response = request.CreateResponse(defaultFailureStatusCode);
            response.WriteString("Failed to process request due to a server failure");

            return response;
        }
    }
}
