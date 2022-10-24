using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class TestFunctionDefinition : FunctionDefinition
    {
        public TestFunctionDefinition(IDictionary<string, BindingMetadata> inputBindings)
        {
            InputBindings = inputBindings.ToImmutableDictionary();
        }

        public override ImmutableArray<FunctionParameter> Parameters { get; }
        public override string PathToAssembly { get; }
        public override string EntryPoint { get; }
        public override string Id { get; }
        public override string Name { get; }
        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }
        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; }
    }
}