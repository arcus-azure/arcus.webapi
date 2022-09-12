using Microsoft.Azure.Functions.Worker;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class TestBindingMetadata : BindingMetadata
    {
        public TestBindingMetadata(string name, string type, BindingDirection direction)
        {
            Name = name;
            Type = type;
            Direction = direction;
        }

        public override string Name { get; }
        public override string Type { get; }
        public override BindingDirection Direction { get; }
    }
}