using System;
using System.Text.Json.Serialization;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    public class Order
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [JsonPropertyName("articleNumber")]
        public string ArticleNumber { get; set; }

        [JsonPropertyName("scheduled")]
        public DateTimeOffset Scheduled { get; set; }
    }
}
