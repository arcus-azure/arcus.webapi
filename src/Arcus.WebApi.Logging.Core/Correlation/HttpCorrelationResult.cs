﻿using System;
using System.Diagnostics;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents the result of the <see cref="HttpCorrelationTemplate{THttpRequest,THttpResponse}.TrySettingCorrelationFromRequest"/>
    /// whether the incoming HTTP request was successfully correlated into the application or not.
    /// </summary>
    public class HttpCorrelationResult : IDisposable
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IOperationHolder<RequestTelemetry> _operationHolder;

        private HttpCorrelationResult(bool isSuccess, string requestId, string errorMessage)
        {
            Guard.For(() => isSuccess && errorMessage != null, new ArgumentException("Cannot create a successful HTTP correlation result with an error user message", nameof(errorMessage)));
            Guard.For(() => !isSuccess && requestId != null, new ArgumentException("Cannot create a failed HTTP correlation result with a request ID", nameof(requestId)));

            RequestId = requestId;
            ErrorMessage = errorMessage;
            IsSuccess = isSuccess;
        }

        private HttpCorrelationResult(
            CorrelationInfo correlationInfo,
            IOperationHolder<RequestTelemetry> operationHolder,
            TelemetryClient client)
        {
            _telemetryClient = client;
            _operationHolder = operationHolder;
            CorrelationInfo = correlationInfo;
            IsSuccess = true;
        }

        /// <summary>
        /// Gets the possible available HTTP request header value, representing the upstream service.
        /// </summary>
        /// <remarks>
        ///     This value can be missing even in a successful result, as the initial HTTP request doesn't have such a header.
        /// </remarks>
        public string RequestId { get; }

        /// <summary>
        /// Gets the error message that describes why the HTTP correlation process failed on the current HTTP request.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the value indicating whether or not this result represents a successful HTTP correlation on the current HTTP request.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the determined HTTP correlation for this result.
        /// </summary>
        internal CorrelationInfo CorrelationInfo { get; }

        /// <summary>
        /// Creates an <see cref="HttpCorrelationResult"/> representing a successful HTTP correlation on the current HTTP request.
        /// </summary>
        /// <param name="requestId">
        ///     The possible header value of the incoming HTTP request header, representing the upstream service.
        ///     When available, this value will be send back to the user.
        /// </param>
        public static HttpCorrelationResult Success(string requestId)
        {
            return new HttpCorrelationResult(isSuccess: true, requestId, errorMessage: null);
        }

        /// <summary>
        /// Creates an <see cref="HttpCorrelationResult"/> representing a failed HTTP correlation on the current HTTP request.
        /// </summary>
        /// <param name="errorMessage">The error user message describing why the HTTP correlation process failed on the current HTTP request.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="errorMessage"/> is blank.</exception>
        public static HttpCorrelationResult Failure(string errorMessage)
        {
            Guard.NotNullOrWhitespace(errorMessage, nameof(errorMessage), "Requires an error user message that describes why the HTTP correlation process failed on the current HTTP request");
            return new HttpCorrelationResult(isSuccess: false, requestId: null, errorMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="transactionId"></param>
        /// <param name="operationParentId"></param>
        /// <returns></returns>
        public static HttpCorrelationResult Success(TelemetryClient client, string transactionId, string operationParentId)
        {
            var telemetry = new RequestTelemetry();
            telemetry.Context.Operation.Id = transactionId;
            telemetry.Context.Operation.ParentId = operationParentId;

            IOperationHolder<RequestTelemetry> operationHolder = client.StartOperation(telemetry);
            var correlationInfo = new CorrelationInfo(telemetry.Id, transactionId, operationParentId);

            return new HttpCorrelationResult(correlationInfo, operationHolder, client);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Activity activity = Activity.Current;
            if (activity != null && activity.OperationName == "ActivityCreatedByHostingDiagnosticListener")
            {
                activity.Stop();
            }

            if (_telemetryClient != null)
            {
                _telemetryClient.TelemetryConfiguration.DisableTelemetry = true;
            }

            _operationHolder?.Dispose();

            if (_telemetryClient != null)
            {
                _telemetryClient.TelemetryConfiguration.DisableTelemetry = false;
            }
        }
    }
}