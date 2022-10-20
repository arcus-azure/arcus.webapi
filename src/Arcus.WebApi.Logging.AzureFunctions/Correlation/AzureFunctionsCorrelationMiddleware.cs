using System;
using System.Net;
using System.Threading.Tasks;
using Arcus.WebApi.Logging.Core.Correlation;
using GuardNet;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// Correlate the incoming request with the outgoing response by using previously configured <see cref="HttpCorrelationInfoOptions"/>.
    /// </summary>
    public class AzureFunctionsCorrelationMiddleware : IFunctionsWorkerMiddleware
    {
        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext" /> for the current invocation.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A <see cref="Task" /> that represents the asynchronous invocation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> or <paramref name="next"/> is <c>nul</c>.</exception>
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            Guard.NotNull(context, nameof(context), "Requires a function context instance of the current Azure Function invocation to HTTP correlate the HTTP request");
            Guard.NotNull(next, nameof(next), "Requires a 'next' function to chain this HTTP correlation middleware to the next action in the HTTP request pipeline");

            var service = context.InstanceServices.GetRequiredService<AzureFunctionsHttpCorrelation>();
            HttpRequestData request = await DetermineHttpRequestAsync(context);

            using (HttpCorrelationResult result = service.TrySettingCorrelationFromRequest(request, context.InvocationId))
            {
                if (result.IsSuccess)
                {
                    try
                    {
                        await next(context);
                    }
                    finally
                    {
                        HttpResponseData response = context.GetHttpResponseData();
                        service.SetCorrelationHeadersInResponse(response, result);
                    }
                }
                else
                {
                    ILogger<AzureFunctionsCorrelationMiddleware> logger = context.GetLogger<AzureFunctionsCorrelationMiddleware>();
                    logger?.LogError("Unable to correlate the incoming request, returning 400 BadRequest (reason: {ErrorMessage})", result.ErrorMessage);

                    HttpResponseData response = request.CreateResponse(HttpStatusCode.BadRequest);
                    await response.WriteStringAsync(result.ErrorMessage);
                    context.GetInvocationResult().Value = response;
                }
            }
        }

        private static async Task<HttpRequestData> DetermineHttpRequestAsync(FunctionContext context)
        {
            HttpRequestData request = await context.GetHttpRequestDataAsync();
            if (request is null)
            {
                throw new InvalidOperationException(
                    "Cannot determine HTTP request in HTTP correlation middleware, this probably means that their is no HTTP trigger function triggering this invocation."
                    + "Please only use the HTTP correlation in HTTP scenarios");
            }

            return request;
        }
    }
}
