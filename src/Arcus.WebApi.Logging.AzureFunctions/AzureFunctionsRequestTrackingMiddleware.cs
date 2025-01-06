using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Core.Logging;
using Arcus.WebApi.Logging.Core.RequestTracking;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging.AzureFunctions
{
    /// <summary>
    /// Represents a middleware component that tracks a HTTP request in an Azure Functions environment.
    /// </summary>
    public class AzureFunctionsRequestTrackingMiddleware : RequestTrackingTemplate, IFunctionsWorkerMiddleware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsRequestTrackingMiddleware" /> class.
        /// </summary>
        public AzureFunctionsRequestTrackingMiddleware(RequestTrackingOptions options) : base(options)
        {
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.Azure.Functions.Worker.FunctionContext" /> for the current invocation.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous invocation.</returns>
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            ILogger logger = context.GetLogger<AzureFunctionsRequestTrackingMiddleware>();

            HttpRequestData request = await context.GetHttpRequestDataAsync();

            if (request is null || IsRequestPathOmitted(PathString.FromUriComponent(request.Url), logger))
            {
                await next(context);
            }
            else
            {
                if (Options.IncludeRequestBody)
                {
                    request = await EnableHttpRequestBufferingAsync(context);
                }

                string requestBody = await GetPotentialRequestBodyAsync(request, logger);
                using (var measurement = DurationMeasurement.Start())
                {
                    try
                    {
                        await next(context);
                    }
                    finally
                    {
                        HttpResponseData response = context.GetHttpResponseData();
                        var attributeTrackedStatusCodes = Enumerable.Empty<StatusCodeRange>();

                        if (response != null && AllowedToTrackStatusCode((int) response.StatusCode, attributeTrackedStatusCodes, logger))
                        {
                            string responseBody = await GetPotentialResponseBodyAsync(response, logger);
                            LogRequest(requestBody, responseBody, request, response, measurement, logger); 
                        }
                    } 
                }
            }
        }

        private static async Task<HttpRequestData> EnableHttpRequestBufferingAsync(FunctionContext context)
        {
            BindingMetadata bindingMetadata = context.FunctionDefinition.InputBindings.Values.FirstOrDefault(a => a.Type == "httpTrigger") ?? throw new InvalidOperationException(
                    "Cannot enable HTTP request body buffering because it cannot find the Azure Functions' HTTP trigger input binding representing the HTTP request");
            InputBindingData<HttpRequestData> bindingData = await context.BindInputAsync<HttpRequestData>(bindingMetadata);
            bindingData.Value = new BufferedHttpRequestData(bindingData.Value);

            return bindingData.Value;
        }

        private async Task<string> GetPotentialRequestBodyAsync(HttpRequestData request, ILogger logger)
        {
            if (Options.IncludeRequestBody)
            {
                string requestBody = await GetBodyAsync(request.Body, Options.RequestBodyBufferSize, "Request", logger);
                string sanitizedBody = SanitizeRequestBody(request, requestBody);

                return sanitizedBody;
            }

            return null;
        }

        /// <summary>
        /// Extracts information from the HTTP <paramref name="requestBody"/> to include in the request tracking context.
        /// </summary>
        /// <param name="request">The current HTTP request.</param>
        /// <param name="requestBody">The body of the current HTTP request.</param>
        /// <remarks>Override this method if you want to sanitize or remove sensitive information from the request body so that it won't be logged.</remarks>
        protected virtual string SanitizeRequestBody(HttpRequestData request, string requestBody)
        {
            return requestBody;
        }

        private async Task<string> GetPotentialResponseBodyAsync(HttpResponseData response, ILogger logger)
        {
            if (Options.IncludeResponseBody)
            {
                // Response body is already a memory stream.

                string responseBody = await GetBodyAsync(response.Body, Options.ResponseBodyBufferSize, "Response", logger);
                string sanitizedBody = SanitizeResponseBody(response, responseBody);

                return sanitizedBody;
            }

            return null;
        }

        /// <summary>
        /// Extracts information from the HTTP <paramref name="responseBody"/> to include in the request tracking context.
        /// </summary>
        /// <param name="response">The current HTTP response.</param>
        /// <param name="responseBody">The body of the current HTTP response.</param>
        /// <remarks>Override this method if you want to sanitize or remove sensitive information from the response body so that it won't be logged.</remarks>
        protected virtual string SanitizeResponseBody(HttpResponseData response, string responseBody)
        {
            return responseBody;
        }

        private void LogRequest(string requestBody, string responseBody, HttpRequestData request, HttpResponseData response, DurationMeasurement duration, ILogger logger)
        {
            Dictionary<string, StringValues> requestHeaders = request.Headers.ToDictionary(h => h.Key, h => new StringValues(h.Value.ToArray()));

            Dictionary<string, object> logContext = CreateTelemetryContext(requestBody, responseBody, requestHeaders, logger);
#if NET6_0
            logger.LogRequest(request, response.StatusCode, duration, logContext);
#else
            logger.LogWarning(MessageFormats.RequestFormat,
                RequestLogEntry.CreateForHttpRequest(
                    request.Method,
                    request.Url.Scheme,
                    request.Url.Host,
                    request.Url.AbsolutePath,
                    operationName: null,
                    (int) response.StatusCode,
                    duration.StartTime,
                    duration.Elapsed,
                    logContext));
#endif
        }
    }
}
