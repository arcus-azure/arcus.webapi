using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Hosting.AzureFunctions.Formatting
{
    /// <summary>
    /// Represents an Azure Functions middleware component to only allow JSON formatted HTTP requests.
    /// </summary>
    /// <remarks>
    ///     Alternative for as of yet unsupported output formatting in Azure Functions.
    /// </remarks>
    public class AzureFunctionsJsonFormattingMiddleware : IFunctionsWorkerMiddleware
    {
        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.Azure.Functions.Worker.FunctionContext" /> for the current invocation.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous invocation.</returns>
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            HttpRequestData request = await DetermineHttpRequestAsync(context);
            ILogger logger = context.GetLogger<AzureFunctionsJsonFormattingMiddleware>();

            if (IsJson(request, logger) && AcceptsJson(request, logger))
            {
                await next(context);
            }
            else
            {
                HttpResponseData response = request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
                await response.WriteStringAsync("Could not process current request because the request body is not JSON and/or could not accept JSON as response");
                context.GetInvocationResult().Value = response;
            }
        }

        private static async Task<HttpRequestData> DetermineHttpRequestAsync(FunctionContext context)
        {
            HttpRequestData request = await context.GetHttpRequestDataAsync();
            if (request is null)
            {
                throw new InvalidOperationException(
                    "Cannot determine HTTP request in HTTP JSON formatting middleware, this probably means that their is no HTTP trigger function triggering this invocation."
                    + "Please only use the HTTP JSON formatting in HTTP scenarios");
            }

            return request;
        }

        private static bool IsJson(HttpRequestData request, ILogger logger)
        {
            if (request.Body is null || request.Body.Length <= 0)
            {
                return true;
            }

            if (request.Headers.TryGetValues("Content-Type", out IEnumerable<string> headerValues))
            {
                bool isJson = headerValues.Any(value => value == "application/json");
                if (!isJson)
                {
                    logger.LogError("Could not process current request because the HTTP request body is not JSON (Content-Type: {ContentType})", string.Join(", ", headerValues));
                }

                return isJson;
            }

            logger.LogError("Could not process current request because the HTTP request body does not have a Content-Type header");
            return false;
        }

        private static bool AcceptsJson(HttpRequestData request, ILogger logger)
        {
            if (request.Headers.TryGetValues("Accept", out IEnumerable<string> headerValues))
            {
                if (headerValues.All(value => value != "application/json"))
                {
                    logger.LogError("Could not process current request because the HTTP request does not allow JSON as response (Accept: {Accept})", string.Join(", ", headerValues));
                    return false;
                }
            }

            return true;
        }
    }
}
