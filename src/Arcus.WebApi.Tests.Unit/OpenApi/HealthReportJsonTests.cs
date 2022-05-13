using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Bogus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.OpenApi
{
    public class HealthReportJsonTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void MicrosoftReport_ToJsonReport_RemovesException()
        {
            // Arrange
            IDictionary<string, object> data = 
                BogusGenerator.Lorem.Words()
                    .Select(word => new KeyValuePair<string, object>(word, BogusGenerator.Lorem.Word()))
                    .ToDictionary(item => item.Key, item => item.Value);

            var entry = new HealthReportEntry(
                BogusGenerator.PickRandom<HealthStatus>(), 
                BogusGenerator.Lorem.Sentence(), 
                duration: BogusGenerator.Date.Timespan(), 
                BogusGenerator.System.Exception(), 
                new ReadOnlyDictionary<string, object>(data));

            var entries = new Dictionary<string, HealthReportEntry> { ["sample"] = entry };
            var report = new HealthReport(
                new ReadOnlyDictionary<string, HealthReportEntry>(entries),
                totalDuration: BogusGenerator.Date.Timespan());

            // Act
            HealthReportJson json = HealthReportJson.FromHealthReport(report);

            // Assert
            HealthReport actual = HealthReportJson.ToHealthReport(json);
            HealthReportEntry actualEntry = Assert.Single(actual.Entries.Values);
            Assert.NotEqual(entry, actualEntry);
            Assert.Equal(entry.Status, actualEntry.Status);
            Assert.Equal(entry.Data, actualEntry.Data);
            Assert.Equal(entry.Description, actualEntry.Description);
            Assert.Equal(entry.Duration, actualEntry.Duration);
            Assert.Null(actualEntry.Exception);
            Assert.Equal(report.Status, actual.Status);
            Assert.Equal(report.TotalDuration, actual.TotalDuration);
        }

        [Fact]
        public void ToMicrosoftReport_WithoutInstance_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => HealthReportJson.ToHealthReport(report: null));
        }

        [Fact]
        public void FromMicrosoftReport_WithoutInstance_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => HealthReportJson.FromHealthReport(report: null));
        }
    }
}
