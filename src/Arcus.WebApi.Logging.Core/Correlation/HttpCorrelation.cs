using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

// ReSharper disable once CheckNamespace
namespace Arcus.WebApi.Logging.Correlation
{
    /// <summary>
    /// Provides the functionality to correlate HTTP requests and responses according to configured <see cref="CorrelationInfoOptions"/>,
    /// using the <see cref="ICorrelationInfoAccessor"/> to expose the result.
    /// </summary>
    /// <seealso cref="HttpCorrelationInfoAccessor"/>
    public class HttpCorrelation : ICorrelationInfoAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpCorrelationInfoOptions _options;
        private readonly ICorrelationInfoAccessor<CorrelationInfo> _correlationInfoAccessor;
        private readonly ILogger<HttpCorrelation> _logger;

        private static readonly Regex RequestIdRegex = 
            new Regex(@"^(\|)?([a-zA-Z0-9\-]+(\.[a-zA-Z0-9\-]+)?)+(_|\.)?$", RegexOptions.Compiled, matchTimeout: TimeSpan.FromSeconds(1));

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelation"/> class.
        /// </summary>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="correlationInfoAccessor">The instance to set and retrieve the <see cref="CorrelationInfo"/> instance.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <param name="httpContextAccessor">The instance to have access to the current HTTP context.</param>
        /// <exception cref="ArgumentNullException">When any of the parameters are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="options"/> doesn't contain a non-<c>null</c> <see cref="IOptions{TOptions}.Value"/></exception>
        public HttpCorrelation(
            IOptions<HttpCorrelationInfoOptions> options,
            IHttpContextAccessor httpContextAccessor,
            ICorrelationInfoAccessor<CorrelationInfo> correlationInfoAccessor,
            ILogger<HttpCorrelation> logger)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the correlation process");
            Guard.NotNull(httpContextAccessor, nameof(httpContextAccessor), "Requires a HTTP context accessor to get the current HTTP context");
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor), "Requires a correlation info instance to set and retrieve the correlation information");
            Guard.NotNull(options.Value, nameof(options.Value), "Requires a value in the set of options to configure the correlation process");

            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
            _correlationInfoAccessor = correlationInfoAccessor;
            _logger = logger ?? NullLogger<HttpCorrelation>.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelation"/> class.
        /// </summary>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="correlationInfoAccessor">The instance to set and retrieve the <see cref="CorrelationInfo"/> instance.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <param name="httpContextAccessor">The instance to have access to the current HTTP context.</param>
        /// <exception cref="ArgumentNullException">When any of the parameters are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="options"/> doesn't contain a non-<c>null</c> <see cref="IOptions{TOptions}.Value"/></exception>
        [Obsolete("Use the constructor overload with the " + nameof(HttpCorrelationInfoOptions) + " instead")]
        public HttpCorrelation(
            IOptions<CorrelationInfoOptions> options,
            IHttpContextAccessor httpContextAccessor,
            ICorrelationInfoAccessor<CorrelationInfo> correlationInfoAccessor,
            ILogger<HttpCorrelation> logger)
            : this(Options.Create(CreateHttpCorrelationOptions(options?.Value)), httpContextAccessor, correlationInfoAccessor, logger)
        {
        }

        private static HttpCorrelationInfoOptions CreateHttpCorrelationOptions(CorrelationInfoOptions options)
        {
            if (options is null)
            {
                return new HttpCorrelationInfoOptions();
            }
            
            return new HttpCorrelationInfoOptions
            {
                Operation =
                {
                    GenerateId = options.Operation.GenerateId,
                    HeaderName = options.Operation.HeaderName,
                    IncludeInResponse = options.Operation.IncludeInResponse
                },
                Transaction =
                {
                    GenerateId = options.Transaction.GenerateId,
                    HeaderName = options.Transaction.HeaderName,
                    IncludeInResponse = options.Transaction.IncludeInResponse,
                    AllowInRequest = options.Transaction.AllowInRequest,
                    GenerateWhenNotSpecified = options.Transaction.GenerateWhenNotSpecified
                }
            };
        }

        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            return _correlationInfoAccessor.GetCorrelationInfo();
        }

        /// <summary>
        /// Sets the current correlation information for this context.
        /// </summary>
        /// <param name="correlationInfo">The correlation model to set.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="correlationInfo"/> is <c>null</c>.</exception>
        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
            Guard.NotNull(correlationInfo, nameof(correlationInfo));
            _correlationInfoAccessor.SetCorrelationInfo(correlationInfo);
        }

        /// <summary>
        /// Correlate the current HTTP request according to the previously configured <see cref="CorrelationInfoOptions"/>;
        /// returning an <paramref name="errorMessage"/> when the correlation failed.
        /// </summary>
        /// <param name="errorMessage">The failure message that describes why the correlation of the HTTP request wasn't successful.</param>
        /// <returns>
        ///     <para>[true] when the HTTP request was successfully correlated and the HTTP response was altered accordingly;</para>
        ///     <para>[false] there was a problem with the correlation, describing the failure in the <paramref name="errorMessage"/>.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the given <see cref="HttpContext"/> is not available to correlate the request with the response.</exception>
        /// <exception cref="ArgumentException">Thrown when the given <see cref="HttpContext"/> doesn't have any response headers to set the correlation headers.</exception>
        public bool TryHttpCorrelate(out string errorMessage)
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            Guard.NotNull(httpContext, nameof(httpContext), "Requires a HTTP context from the HTTP context accessor to start correlating the HTTP request");
            Guard.For<ArgumentException>(() => httpContext.Response is null, "Requires a 'Response'");
            Guard.For<ArgumentException>(() => httpContext.Response.Headers is null, "Requires a 'Response' object with headers");

            if (httpContext.Request.Headers.TryGetValue(_options.Transaction.HeaderName, out StringValues transactionIds))
            {
                if (!_options.Transaction.AllowInRequest)
                {
                    _logger.LogError("No correlation request header '{HeaderName}' for transaction ID was allowed in request", _options.Transaction.HeaderName);
                    errorMessage = $"No correlation transaction ID request header '{_options.Transaction.HeaderName}' was allowed in the request";
                    return false;
                }

                _logger.LogTrace("Correlation request header '{HeaderName}' found with transaction ID '{TransactionId}'", _options.Transaction.HeaderName, transactionIds);
            }

            string transactionId = DetermineTransactionId(transactionIds);
            string operationId, operationParentId = null;

            if (_options.UpstreamService.ExtractFromRequest && TryGetUpstreamOperationId(httpContext, out operationParentId))
            {
                operationId = ExtractOperationIdFromOperationParentId(operationParentId);
                if (operationId is null)
                {
                    _logger.LogError("No operation parent ID with a correct format could be found in the request header '{HeaderName}' which means no operation ID could be extracted", _options.UpstreamService.OperationParentIdHeaderName);
                    errorMessage = $"No correlation operation ID could be extracted from upstream service's '{_options.UpstreamService.OperationParentIdHeaderName}' request header";
                    return false;
                }
            }
            else
            {
                operationId = DetermineOperationIdWithoutUpstreamService(httpContext);
            }

            httpContext.Features.Set(new CorrelationInfo(operationId, transactionId, operationParentId));
            AddCorrelationResponseHeaders(httpContext);

            errorMessage = null;
            return true;
        }

        private string DetermineTransactionId(StringValues transactionIds)
        {
            var alreadyPresentTransactionId = transactionIds.ToString();
            if (String.IsNullOrWhiteSpace(alreadyPresentTransactionId))
            {
                if (_options.Transaction.GenerateWhenNotSpecified)
                {
                    _logger.LogTrace("No transactional ID was found in the request, generating one...");
                    string newlyGeneratedTransactionId = _options.Transaction.GenerateId();

                    if (String.IsNullOrWhiteSpace(newlyGeneratedTransactionId))
                    {
                        throw new InvalidOperationException(
                            $"Correlation cannot use function '{nameof(_options.Transaction.GenerateId)}' to generate an transaction ID because the resulting ID value is blank");
                    }

                    _logger.LogTrace("Generated '{TransactionId}' as transactional correlation ID", newlyGeneratedTransactionId);
                    return newlyGeneratedTransactionId;
                }

                _logger.LogTrace("No transactional correlation ID found in request header '{HeaderName}' but since the correlation options specifies that no transactional ID should be generated, there will be no ID present", 
                    _options.Transaction.HeaderName);
                
                return null;
            }

            _logger.LogTrace("Found transactional correlation ID '{TransactionId}' in request header '{HeaderName}'", alreadyPresentTransactionId, _options.Transaction.HeaderName);
            return alreadyPresentTransactionId;
        }

        private bool TryGetUpstreamOperationId(HttpContext context, out string operationParentId)
        {
            if (context.Request.Headers.TryGetValue(_options.UpstreamService.OperationParentIdHeaderName, out StringValues requestId) 
                && !String.IsNullOrWhiteSpace(requestId)
                && requestId.Count > 0
                && MatchesRequestIdFormat(requestId))
            {
                _logger.LogTrace("Found operation parent ID '{OperationParentId}' from upstream service in request's header '{HeaderName}'", requestId, _options.UpstreamService.OperationParentIdHeaderName);
                operationParentId = requestId;
                return true;
            }

            _logger.LogTrace("No operation parent ID found from upstream service in the request's header '{HeaderName}' that matches the expected format: |Guid.", _options.UpstreamService.OperationParentIdHeaderName);
            operationParentId = null;
            return false;
        }

        private bool MatchesRequestIdFormat(string requestId)
        {
            try
            {
                return RequestIdRegex.IsMatch(requestId);
            }
            catch (RegexMatchTimeoutException exception)
            {
                _logger.LogTrace(exception, "Upstream service's '{HeaderName}' was timed-out during regular expression validation", _options.UpstreamService.OperationParentIdHeaderName);
                return false;
            }
        }

        private string ExtractOperationIdFromOperationParentId(string operationParentId)
        {
            if (operationParentId is null)
            {
                return null;
            }
            
            // Returns the root ID from the '|' to the first '.' if any, according to W3C Trace-Context standard
            // Ex. Request-Id=|7515DCD2-6340-41A9-AA4F-0E1DD28055B6.
            //     returns: 7515DCD2-6340-41A9-AA4F-0E1DD28055B6
            
            _logger.LogTrace("Extracting operation ID from operation parent ID '{OperationParentId}' from the upstream service according to W3C Trace-Context standard", operationParentId);
            
            int rootStart;
            if (operationParentId[0] == '|')
            {
                rootStart = 1;
            }
            else
            {
                rootStart = 0;
            }
            
            int rootEnd = operationParentId.IndexOf('.');
            if (rootEnd < 0)
            {
                rootEnd = operationParentId.Length;
            }

            string operationId = operationParentId.Substring(rootStart, rootEnd - rootStart);
            _logger.LogTrace("Extracted operation ID '{OperationId}' from operation parent ID '{OperationParentId}' from the upstream service", operationId, operationParentId);

            return operationId;
        }

        private string DetermineOperationIdWithoutUpstreamService(HttpContext httpContext)
        {
            if (String.IsNullOrWhiteSpace(httpContext.TraceIdentifier))
            {
                _logger.LogTrace("No unique trace identifier ID was found in the request, generating one...");
                string operationId = _options.Operation.GenerateId();
                
                if (String.IsNullOrWhiteSpace(operationId))
                {
                    throw new InvalidOperationException(
                        $"Correlation cannot use '{nameof(_options.Operation.GenerateId)}' to generate an operation ID because the resulting ID value is blank");
                }

                _logger.LogTrace("Generated '{OperationId}' as unique operation correlation ID", operationId);
                return operationId;
            }

            _logger.LogTrace("Found unique trace identifier ID '{TraceIdentifier}' for operation correlation ID", httpContext.TraceIdentifier);
            return httpContext.TraceIdentifier;
        }

        private void AddCorrelationResponseHeaders(HttpContext httpContext)
        {
            if (_options.Operation.IncludeInResponse)
            {
                _logger.LogTrace("Prepare for the operation correlation ID to be included in the response...");
                httpContext.Response.OnStarting(() =>
                {
                    CorrelationInfo correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
                    AddResponseHeader(httpContext, _options.Operation.HeaderName, correlationInfo.OperationId);
                    return Task.CompletedTask;
                });
            }

            if (_options.Transaction.IncludeInResponse)
            {
                _logger.LogTrace("Prepare for the transactional correlation ID to be included in the response...");
                httpContext.Response.OnStarting(() =>
                {
                    CorrelationInfo correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
                    AddResponseHeader(httpContext, _options.Transaction.HeaderName, correlationInfo.TransactionId);
                    return Task.CompletedTask;
                });
            }
        }

        private void AddResponseHeader(HttpContext httpContext, string headerName, string headerValue)
        {
            _logger.LogTrace("Setting correlation response header '{HeaderName}' to '{CorrelationId}'", headerName, headerValue);
            httpContext.Response.Headers[headerName] = headerValue;
        }
    }
}
