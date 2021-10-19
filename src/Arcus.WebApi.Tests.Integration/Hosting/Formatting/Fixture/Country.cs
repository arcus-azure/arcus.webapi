using System.Text.Json.Serialization;

namespace Arcus.WebApi.Tests.Integration.Hosting.Formatting.Fixture
{
    public class Country
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("code")]
        public CountryCode Code { get; set; }
    }
}
