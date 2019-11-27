﻿using System;
using Correlate;

namespace Arcus.WebApi.Correlation
{
    /// <summary>
    /// Represents the correlation ID information on the incoming requests and outgoing responses.
    /// </summary>
    internal class Correlation : CorrelationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Correlation"/> class.
        /// </summary>
        internal Correlation()
        {
            // TODO: control how the 'Request-ID' gets generated.
            RequestId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets the unique ID information of the request.
        /// </summary>
        internal string RequestId { get; }
    }
}