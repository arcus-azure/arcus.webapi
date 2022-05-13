using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Represents an entry in a <see cref="HealthReportJson"/>.
    /// Corresponds to the result of a single <see cref="IHealthCheck"/>.
    /// </summary>
    public struct HealthReportEntryJson
    {
        /// <summary>
        /// Gets additional key-value pairs describing the health of the component.
        /// </summary>
        public IDictionary<string, object> Data { get; set; }

        /// <summary>
        /// Gets a human-readable description of the status of the component that was checked.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the health check execution duration.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets the health status of the component that was checked.
        /// </summary>
        public HealthStatus Status { get; set; }

        /// <summary>
        /// Gets the tags associated with the health check.
        /// </summary>
        public IEnumerable<string> Tags { get; set; }

        /// <summary>
        /// Implicit operation to work seamlessly with the <see cref="HealthReportEntry"/> once the health report is being finalized and needs to be sent.
        /// </summary>
        /// <param name="entry">The entry of the created health report, representing a single <see cref="IHealthCheck"/> with exception details.</param>
        public static implicit operator HealthReportEntryJson(HealthReportEntry entry)
        {
            return new HealthReportEntryJson
            {
                Data = entry.Data.ToDictionary(item => item.Key, item => item.Value),
                Description = entry.Description,
                Duration = entry.Duration,
                Status = entry.Status,
                Tags = entry.Tags
            };
        }

        /// <summary>
        /// Implicit operation to work seamlessly with the Microsoft <see cref="HealthReportEntry"/> once the serialization is complete.
        /// </summary>
        /// <param name="entry">The JSON data-transfer object, representing a single <see cref="IHealthCheck"/> without the exception details.</param>
        public static implicit operator HealthReportEntry(HealthReportEntryJson entry)
        {
            return new HealthReportEntry(
                entry.Status,
                entry.Description,
                entry.Duration,
                exception: null,
                new ReadOnlyDictionary<string, object>(entry.Data),
                entry.Tags);
        }
    }
}