using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Represents an alternative <see cref="HealthReport"/> model without the exception details so it can be exposed safely with OpenAPI.
    /// </summary>
    /// <remarks>
    ///     This model should not be used within the domain and is just available for the serialization between trust boundaries.
    ///     Use the <see cref="FromHealthReport"/> and <see cref="ToHealthReport"/> to switch between the two.
    /// </remarks>
    public class HealthReportJson
    {
        /// <summary>
        /// Gets a dictionary containing the results from each health check.
        /// </summary>
        public IDictionary<string, HealthReportEntryJson> Entries { get; set; }

        /// <summary>
        /// Gets a <see cref="HealthStatus"/> representing the aggregate status of all the health checks.
        /// The value of <see cref="Status"/> will be the most severe status reported by a health check.
        /// If no checks were executed, the value is always <see cref="HealthStatus.Healthy"/>.
        /// </summary>
        public HealthStatus Status { get; set; } = HealthStatus.Healthy;

        /// <summary>
        /// Gets the time the health check service took to execute.
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// Creates a JSON data-transfer object from the given <paramref name="report"/>.
        /// </summary>
        /// <param name="report">The finalized health report.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="report"/> is <c>null</c>.</exception>
        public static HealthReportJson FromHealthReport(HealthReport report)
        {
            Guard.NotNull(report, nameof(report), "Requires a Microsoft HealthReport instance to convert to a JSON instance without the exception details");

            return new HealthReportJson
            {
                Entries = report.Entries.ToDictionary(item => item.Key, item => (HealthReportEntryJson) item.Value),
                Status = report.Status,
                TotalDuration = report.TotalDuration
            };
        }

        /// <summary>
        /// Creates a JSON data-transfer object from the given <paramref name="report"/>.
        /// </summary>
        /// <param name="report">The finalized health report.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="report"/> is <c>null</c>.</exception>
        public static HealthReport ToHealthReport(HealthReportJson report)
        {
            Guard.NotNull(report, nameof(report), "Requires a Microsoft HealthReport instance to convert to a JSON instance without the exception details");

            IDictionary<string, HealthReportEntry> entries =
                report.Entries.ToDictionary(item => item.Key, item => (HealthReportEntry) item.Value);

            return new HealthReport(
                new ReadOnlyDictionary<string, HealthReportEntry>(entries),
                report.TotalDuration);
        }

        /// <summary>
        /// Implicit operation to work seamlessly with the Microsoft <see cref="HealthReport"/> once the health report is finalized.
        /// </summary>
        /// <param name="report">The finalized health report.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="report"/> is <c>null</c>.</exception>
        public static implicit operator HealthReportJson(HealthReport report)
        {
            Guard.NotNull(report, nameof(report), "Requires a Microsoft HealthReport instance to convert to a JSON instance without the exception details");
            return FromHealthReport(report);
        }

        /// <summary>
        /// Implicit operation to work seamlessly with the Microsoft <see cref="HealthReport"/> once the serialization is complete.
        /// </summary>
        /// <param name="report">The JSON data-transfer object that needs to be converted back to the Microsoft instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="report"/> is <c>null</c>.</exception>
        public static implicit operator HealthReport(HealthReportJson report)
        {
            Guard.NotNull(report, nameof(report), "Requires a JSON HealthReport instance to convert to a Microsoft instance with the exception details");
            return ToHealthReport(report);
        }
    }
}
