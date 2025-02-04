﻿using System;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents the correlation options specific for the operation ID.
    /// </summary>
    public class HttpCorrelationInfoOperationOptions
    {
        private string _headerName = HttpCorrelationProperties.OperationIdHeaderName;
        private Func<string> _generateId = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets whether to include the operation ID in the response.
        /// </summary>
        /// <remarks>
        ///     A common use case is to disable tracing info in edge services, so that such details are not exposed to the outside world.
        /// </remarks>
        public bool IncludeInResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets the header that will contain the response operation ID.
        /// </summary>
        public string HeaderName
        {
            get => _headerName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Correlation operation header cannot be blank", nameof(value));
                }

                _headerName = value;
            }
        }

        /// <summary>
        /// Gets or sets the function to generate the operation ID when the <see cref="P:Arcus.Observability.Correlation.CorrelationInfoOperationOptions.IncludeInResponse" /> is set to <c>true</c> (default: <c>true</c>).
        /// </summary>
        /// <remarks>
        ///     This is only used when the <see cref="HttpCorrelationInfoOptions.Format"/> is set to <see cref="HttpCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        public Func<string> GenerateId
        {
            get => _generateId;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value), "Correlation function to generate an operation ID cannot be 'null'");
                }

                _generateId = value;
            }
        }
    }
}