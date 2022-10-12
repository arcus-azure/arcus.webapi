using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text.RegularExpressions;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents a template for any HTTP-related systems to extract and set HTTP correlation throughout the application.
    /// </summary>
    public abstract class HttpCorrelationTemplate<THttpRequest, THttpResponse> 
        where THttpRequest : class 
        where THttpResponse : class
    {
        private readonly HttpCorrelationInfoOptions _options;
        private readonly IHttpCorrelationInfoAccessor _correlationInfoAccessor;
        private readonly ILogger _logger;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly Regex RequestIdRegex = 
            new Regex(@"^(\|)?([a-zA-Z0-9\-]+(\.[a-zA-Z0-9\-]+)?)+(_|\.)?$", RegexOptions.Compiled, matchTimeout: TimeSpan.FromSeconds(1));

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelationTemplate{THttpRequest,THttpResponse}" /> class.
        /// </summary>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="correlationInfoAccessor">The instance to set and retrieve the <see cref="CorrelationInfo"/> instance.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="correlationInfoAccessor"/> is <c>null</c>.</exception>
        protected HttpCorrelationTemplate(
            HttpCorrelationInfoOptions options, 
            IHttpCorrelationInfoAccessor correlationInfoAccessor,
            ILogger logger)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the correlation process");
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor), "Requires a correlation info instance to set and retrieve the correlation information");

            _options = options;
            _correlationInfoAccessor = correlationInfoAccessor;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Correlate the current HTTP request according to the previously configured <see cref="HttpCorrelationInfoOptions"/>;
        /// returning an <see cref="HttpCorrelationResult"/> reflecting the success status of the operation.
        /// </summary>
        /// <returns>
        ///     An <see cref="HttpCorrelationResult"/> that reflects whether or not the incoming HTTP request was successfully correlated into a <see cref="CorrelationInfo"/> model
        ///     that is set into the application's <see cref="IHttpCorrelationInfoAccessor"/>.
        /// </returns>
        /// <remarks>
        ///     This part is only half of the HTTP correlation operation, with the returned result,
        ///     the HTTP response should be updated with <see cref="SetCorrelationHeadersInResponse"/>.
        /// </remarks>
        /// <param name="request">The incoming HTTP request that contains the HTTP correlation headers.</param>
        /// <param name="traceIdentifier">The value that identifies the <paramref name="request"/>. When present, this will be used as the operation ID of the HTTP correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        public HttpCorrelationResult TrySettingCorrelationFromRequest(THttpRequest request, string traceIdentifier)
        {
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to determine the HTTP correlation of the application");

            IHeaderDictionary requestHeaders = GetRequestHeaders(request);
            if (requestHeaders is null)
            {
                _logger.LogWarning("No HTTP request headers could be determined from incoming request, please verify if the HTTP correlation template was correctly implemented");
                requestHeaders = new HeaderDictionary();
            }

            if (_options.Format is HttpCorrelationFormat.Hierarchical)
            {
                HttpCorrelationResult result = CorrelateHierarchical(requestHeaders, traceIdentifier);
                return result;
            }

            if (_options.Format is HttpCorrelationFormat.W3C)
            {
                StringValues traceParent = requestHeaders.GetTraceParent();
                if (IsTraceParentHeaderW3CCompliant(traceParent))
                {
                    HttpCorrelationResult result = CorrelateW3CForExistingParent(requestHeaders);
                    return result;
                }
                else
                {
                    HttpCorrelationResult result = CorrelateW3CForNewParent(requestHeaders);
                    return result;
                }
            }

            throw new InvalidOperationException(
                "Could not determine which type of HTTP correlation system to use (Hierarchical or W3C); we recommend to use W3C instead of the deprecated Hierarchical correlation system");
        }

        private HttpCorrelationResult CorrelateW3CForNewParent(IHeaderDictionary requestHeaders)
        {
            Activity newActivity = CreateNewActivity(requestHeaders);
            string transactionId = newActivity.TraceId.ToHexString();
            _logger.LogTrace("Correlation transaction ID '{TransactionId}' found in 'traceparent' HTTP request header", transactionId);

            string operationId = newActivity.SpanId.ToHexString();
            _logger.LogTrace("Correlation operation ID '{OperationId}' generated for incoming HTTP request", operationId);

            newActivity.Start();
            Activity.Current = newActivity;

            _correlationInfoAccessor.SetCorrelationInfo(new CorrelationInfo(operationId, transactionId));
            return HttpCorrelationResult.Success(requestId: null);
        }

        private HttpCorrelationResult CorrelateW3CForExistingParent(IHeaderDictionary requestHeaders)
        {
            Activity newActivity = CreateNewActivity(requestHeaders);

            // Format example:   00-4b1c0c8d608f57db7bd0b13c88ef865e-4c6893cc6c6cad10-00
            // Format structure: 00-<-----trace/transaction-id----->-<span/parent-id>-00 
            string traceParent = requestHeaders.GetTraceParent().TruncateString(55);
            string transactionId = ActivityTraceId.CreateFromString(traceParent.AsSpan(3, 32)).ToHexString();
            _logger.LogTrace("Correlation transaction ID '{TransactionId}' found in 'traceparent' HTTP request header", transactionId);

            var parentSpanId = ActivitySpanId.CreateFromString(traceParent.AsSpan(36, 16));
            string operationParentId = parentSpanId.ToHexString();
            _logger.LogTrace("Correlation operation parent ID '{OperationParentId}' found in 'traceparent' HTTP request header", operationParentId);

            newActivity.SetParentId(ActivityTraceId.CreateFromString(transactionId), parentSpanId);
            newActivity.Start();
            string operationId = newActivity.SpanId.ToHexString();
            _logger.LogTrace("Correlation operation ID '{OperationId}' generated for incoming HTTP request", operationId);
            Activity.Current = newActivity;

            _correlationInfoAccessor.SetCorrelationInfo(new CorrelationInfo(operationId, transactionId, operationParentId));
            return HttpCorrelationResult.Success(traceParent);
        }

        private static Activity CreateNewActivity(IHeaderDictionary requestHeaders)
        {
            Activity currentActivity = Activity.Current;
            var newActivity = new Activity("ActivityCreatedByHostingDiagnosticListener");
            newActivity.TraceStateString = requestHeaders.GetTraceState();

            if (currentActivity is null)
            {
                return newActivity;
            }

            foreach (KeyValuePair<string, string> tag in currentActivity.Tags)
            {
                newActivity.AddTag(tag.Key, tag.Value);
            }

            foreach (KeyValuePair<string, string> baggage in currentActivity.Baggage)
            {
                newActivity.AddBaggage(baggage.Key, baggage.Value);
            }

            const int contextHeaderKeyMaxLength = 50;
            const int contextHeaderValueMaxLength = 1024;

            if (!currentActivity.Baggage.Any())
            {
                string[] baggage1 = requestHeaders.GetCommaSeparatedValues("Correlation-Context");
                if (baggage1 != StringValues.Empty)
                {
                    foreach (string item in baggage1)
                    {
                        string[] parts = item.Split('=');
                        if (parts.Length == 2)
                        {
                            string itemName = parts[0].TruncateString(contextHeaderKeyMaxLength);
                            string itemValue = parts[1].TruncateString(contextHeaderValueMaxLength);
                            currentActivity.AddBaggage(itemName.Trim(), itemValue.Trim());
                        }
                    }
                }
            }

            return newActivity;
        }

        private static bool IsTraceParentHeaderW3CCompliant(StringValues ids)
        {
            if (ids == StringValues.Empty)
            {
                return false;
            }

            string id = ids;
            if (id.Length != 55 
                || ('0' > id[0] || id[0] > '9') 
                && ('a' > id[0] || id[0] > 'f') 
                || ('0' > id[1] || id[1] > '9') 
                && ('a' > id[1] || id[1] > 'f'))
            {
                return false;
            }

            return id[0] != 'f' || id[1] != 'f';
        }

        private HttpCorrelationResult CorrelateHierarchical(IHeaderDictionary requestHeaders, string traceIdentifier)
        {
            if (TryGetTransactionId(requestHeaders, out string alreadyPresentTransactionId))
            {
                if (!_options.Transaction.AllowInRequest)
                {
                    _logger.LogError("No correlation request header '{HeaderName}' for transaction ID was allowed in request", _options.Transaction.HeaderName);
                    return HttpCorrelationResult.Failure($"No correlation transaction ID request header '{_options.Transaction.HeaderName}' was allowed in the request");
                }

                _logger.LogTrace("Correlation request header '{HeaderName}' found with transaction ID '{TransactionId}'", _options.Transaction.HeaderName, alreadyPresentTransactionId);
            }

            string operationId = DetermineOperationId(traceIdentifier);
            string transactionId = DetermineTransactionId(alreadyPresentTransactionId);
            string operationParentId = null;
            string requestId = null;

            if (_options.UpstreamService.ExtractFromRequest)
            {
                if (TryGetRequestId(requestHeaders, _options.UpstreamService.HeaderName, out requestId))
                {
                    operationParentId = ExtractLatestOperationParentIdFromHeader(requestId);
                    if (operationParentId is null)
                    {
                        return HttpCorrelationResult.Failure("No correlation operation parent ID could be extracted from upstream service's request header");
                    }
                }
            }
            else
            {
                operationParentId = _options.UpstreamService.GenerateId();
                requestId = operationParentId;
            }

            _correlationInfoAccessor.SetCorrelationInfo(new CorrelationInfo(operationId, transactionId, operationParentId));
            return HttpCorrelationResult.Success(requestId);
        }

        /// <summary>
        /// Gets the HTTP request headers from the incoming <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The incoming HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        protected abstract IHeaderDictionary GetRequestHeaders(THttpRequest request);

        private bool TryGetTransactionId(IHeaderDictionary requestHeaders, out string alreadyPresentTransactionId)
        {
            return TryGetHeaderValue(requestHeaders, _options.Transaction.HeaderName, out alreadyPresentTransactionId);
        }

        private string DetermineOperationId(string traceIdentifier)
        {
            if (string.IsNullOrWhiteSpace(traceIdentifier))
            {
                _logger.LogTrace("No unique trace identifier ID was found in the request, generating one...");
                string operationId = _options.Operation.GenerateId();
                
                if (string.IsNullOrWhiteSpace(operationId))
                {
                    throw new InvalidOperationException(
                        $"Correlation cannot use '{nameof(_options.Operation.GenerateId)}' to generate an operation ID because the resulting ID value is blank");
                }

                _logger.LogTrace("Generated '{OperationId}' as unique operation correlation ID", operationId);
                return operationId;
            }

            _logger.LogTrace("Found unique trace identifier ID '{TraceIdentifier}' for operation correlation ID", traceIdentifier);
            return traceIdentifier;
        }

        private string DetermineTransactionId(string alreadyPresentTransactionId)
        {
            if (string.IsNullOrWhiteSpace(alreadyPresentTransactionId))
            {
                if (_options.Transaction.GenerateWhenNotSpecified)
                {
                    _logger.LogTrace("No transactional ID was found in the request, generating one...");
                    string newlyGeneratedTransactionId = _options.Transaction.GenerateId();

                    if (string.IsNullOrWhiteSpace(newlyGeneratedTransactionId))
                    {
                        throw new InvalidOperationException(
                            $"Correlation cannot use function '{nameof(_options.Transaction.GenerateId)}' to generate an transaction ID because the resulting ID value is blank");
                    }

                    _logger.LogTrace("Generated '{TransactionId}' as transactional correlation ID", newlyGeneratedTransactionId);
                    return newlyGeneratedTransactionId;
                }

                _logger.LogTrace("No transactional correlation ID found in request header '{HeaderName}' but since the correlation options specifies that no transactional ID should be generated, there will be no ID present", _options.Transaction.HeaderName);
                return null;
            }

            _logger.LogTrace("Found transactional correlation ID '{TransactionId}' in request header '{HeaderName}'", alreadyPresentTransactionId, _options.Transaction.HeaderName);
            return alreadyPresentTransactionId;
        }

        private bool TryGetRequestId(IHeaderDictionary requestHeaders, string headerName, out string requestId)
        {
            if (TryGetHeaderValue(requestHeaders, headerName, out string id)
                && MatchesRequestIdFormat(id, headerName))
            {
                _logger.LogTrace("Found operation parent ID '{OperationParentId}' from upstream service in request's header '{HeaderName}'", id, headerName);
                requestId = id;
                return true;
            }

            _logger.LogTrace("No operation parent ID found from upstream service in the request's header '{HeaderName}' that matches the expected format: |Guid.", headerName);
            requestId = null;
            return false;
        }

        private static bool TryGetHeaderValue(IHeaderDictionary headers, string headerName, out string headerValue)
        {
            (string key, string value) = headers.FirstOrDefault(h => string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                headerValue = value;
                return true;
            }

            headerValue = null;
            return false;
        }

        private string ExtractLatestOperationParentIdFromHeader(string requestId)
        {
            if (requestId is null)
            {
                return null;
            }
            
            // Returns the ID from the last '.' if any, according to W3C Trace-Context standard
            // Ex. Request-Id=|abc.def
            //     returns: def
            
            _logger.LogTrace("Extracting operation parent ID from request ID '{RequestId}' from the upstream service according to W3C Trace-Context standard", requestId);
            if (requestId.Contains("."))
            {
                string[] ids = requestId.Split('.');
                string operationParentId = ids.LastOrDefault(id => !string.IsNullOrWhiteSpace(id));

                _logger.LogTrace("Extracted operation parent ID '{OperationParentId}' from request ID '{RequestId}' from the upstream service", operationParentId, requestId);
                return operationParentId;
            }
            else
            {
                string operationParentId = requestId.TrimStart('|');
                
                _logger.LogTrace("Extracted operation parent ID '{OperationParentId}' from request ID '{RequestId}' from the upstream service", operationParentId, requestId);
                return operationParentId;
            }
        }

        private bool MatchesRequestIdFormat(string requestId, string headerName)
        {
            try
            {
                return RequestIdRegex.IsMatch(requestId);
            }
            catch (RegexMatchTimeoutException exception)
            {
                _logger.LogTrace(exception, "Upstream service's '{HeaderName}' was timed-out during regular expression validation", headerName);
                return false;
            }
        }

        /// <summary>
        /// Sets the necessary HTTP correlation headers in the outgoing <paramref name="response"/>
        /// based on the <paramref name="result"/> of the HTTP correlation extraction from the HTTP request.
        /// </summary>
        /// <param name="response">The outgoing HTTP response.</param>
        /// <param name="result">The result of a HTTP correlation extraction from the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="response"/> or <paramref name="result"/> is <c>null</c>.</exception>
        public void SetCorrelationHeadersInResponse(THttpResponse response, HttpCorrelationResult result)
        {
            Guard.NotNull(response, nameof(response), "Requires a HTTP response to set the HTTP correlation headers");
            Guard.NotNull(result, nameof(result), "Requires a HTTP correlation result to determine to set the HTTP correlation headers in the HTTP request");

            string requestId = result.RequestId;
            CorrelationInfo correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
            
            if (_options.Operation.IncludeInResponse)
            {
                _logger.LogTrace("Prepare for the operation ID to be included in the response...");
                if (string.IsNullOrWhiteSpace(correlationInfo?.OperationId))
                {
                    _logger.LogWarning("No response header was added given no operation ID was found");
                }
                else
                {
                    SetHttpResponseHeader(response, _options.Operation.HeaderName, correlationInfo.OperationId);
                }
            }

            if (_options.UpstreamService.IncludeInResponse)
            {
                _logger.LogTrace("Prepare for the operation parent ID to be included in the response...");

                if (string.IsNullOrWhiteSpace(requestId))
                {
                    _logger.LogWarning("No response header was added given no operation parent ID was found");
                }
                else
                {
                    SetHttpResponseHeader(response, _options.UpstreamService.HeaderName, requestId);
                }
            }

            if (_options.Transaction.IncludeInResponse)
            {
                _logger.LogTrace("Prepare for the transactional correlation ID to be included in the response...");

                if (string.IsNullOrWhiteSpace(correlationInfo?.TransactionId))
                {
                    _logger.LogWarning(
                        "No response header was added given no transactional correlation ID was found");
                }
                else
                {
                    SetHttpResponseHeader(response, _options.Transaction.HeaderName, correlationInfo.TransactionId);
                }
            }
        }

        /// <summary>
        /// Set the <paramref name="headerName"/>, <paramref name="headerValue"/> combination in the outgoing <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The outgoing HTTP response that gets a HTTP correlation header.</param>
        /// <param name="headerName">The HTTP correlation response header name.</param>
        /// <param name="headerValue">The HTTP correlation response header value.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="response"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> or <paramref name="headerValue"/> is blank.</exception>
        protected abstract void SetHttpResponseHeader(THttpResponse response, string headerName, string headerValue);
    }
}
