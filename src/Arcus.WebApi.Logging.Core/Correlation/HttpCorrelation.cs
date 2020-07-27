using System;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

// ReSharper disable once CheckNamespace
namespace Arcus.WebApi.Logging.Correlation
{
    /// <summary>
    /// Provides the functionality to correlate HTTP requests and responses according to configured options.
    /// </summary>
    public class HttpCorrelation
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CorrelationInfoOptions _options;
        private readonly ILogger<HttpCorrelation> _logger;

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
            IOptions<CorrelationInfoOptions> options,
            IHttpContextAccessor httpContextAccessor,
            ICorrelationInfoAccessor correlationInfoAccessor,
            ILogger<HttpCorrelation> logger)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the correlation process");
            Guard.NotNull(httpContextAccessor, nameof(httpContextAccessor), "Requires a HTTP context accessor to get the current HTTP context");
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor), "Requires a correlation info instance to set and retrieve the correlation information");
            Guard.NotNull(logger, nameof(logger), "Requires a logger to write diagnostic messages during the correlation");
            Guard.NotNull(options.Value, nameof(options.Value), "Requires a value in the set of options to configure the correlation process");
            
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
            _logger = logger;

            CorrelationInfoAccessor = correlationInfoAccessor;
        }

        /// <summary>
        /// Gets the instance to set and retrieve the <see cref="CorrelationInfo"/> instance.
        /// </summary>
        public ICorrelationInfoAccessor CorrelationInfoAccessor { get; }

        /// <summary>
        /// Correlate the current HTTP request according to the previously configured correlation options; returning an <paramref name="errorMessage"/> when the correlation failed.
        /// </summary>
        /// <param name="errorMessage">The failure message that describes why the correlation of the HTTP request wasn't successful.</param>
        /// <returns>
        ///     [true] when the HTTP request was successfully correlated and the HTTP response was altered accordingly;
        ///     [false] there was a problem with the correlation, describing the failure in the <paramref name="errorMessage"/>.
        /// </returns>
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

            string operationId = DetermineOperationId(httpContext);
            string transactionId = DetermineTransactionId(transactionIds);
            var correlation = new CorrelationInfo(operationId, transactionId);
            httpContext.Features.Set(correlation);

            AddCorrelationResponseHeaders(httpContext);

            errorMessage = null;
            return true;
        }

        private string DetermineOperationId(HttpContext httpContext)
        {
            if (String.IsNullOrWhiteSpace(httpContext.TraceIdentifier))
            {
                string operationId = _options.Operation.GenerateId();
                if (String.IsNullOrWhiteSpace(operationId))
                {
                    throw new InvalidOperationException(
                        $"Correlation cannot use '{nameof(_options.Operation.GenerateId)}' to generate an operation ID because the resulting ID value is blank");
                }

                return operationId;
            }

            return httpContext.TraceIdentifier;
        }

        private string DetermineTransactionId(StringValues transactionIds)
        {
            if (String.IsNullOrWhiteSpace(transactionIds.ToString()))
            {
                if (_options.Transaction.GenerateWhenNotSpecified)
                {
                    string transactionId = _options.Transaction.GenerateId();
                    if (String.IsNullOrWhiteSpace(transactionId))
                    {
                        throw new InvalidOperationException(
                            $"Correlation cannot use function '{nameof(_options.Transaction.GenerateId)}' to generate an transaction ID because the resulting ID value is blank");
                    }

                    return transactionId;
                }

                return null;
            }

            return transactionIds.ToString();
        }

        private void AddCorrelationResponseHeaders(HttpContext httpContext)
        {
            if (_options.Operation.IncludeInResponse)
            {
                httpContext.Response.OnStarting(() =>
                {
                    CorrelationInfo correlationInfo = CorrelationInfoAccessor.GetCorrelationInfo();
                    AddResponseHeader(httpContext, _options.Operation.HeaderName, correlationInfo.OperationId);
                    return Task.CompletedTask;
                });
            }

            if (_options.Transaction.IncludeInResponse)
            {
                httpContext.Response.OnStarting(() =>
                {
                    CorrelationInfo correlationInfo = CorrelationInfoAccessor.GetCorrelationInfo();
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
