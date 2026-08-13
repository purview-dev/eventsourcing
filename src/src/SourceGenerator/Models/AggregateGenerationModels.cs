using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Helpers;

namespace Purview.EventSourcing.SourceGenerator.Models;

sealed record AggregateGenerationModel(
	bool IsSourceGeneratorEnabled,
	AggregateGenerationContext GenerationContext
)
{
	public ImmutableArray<GeneratorResult<AggregateInfo>> Aggregates { get; set; } = [];

	public ImmutableArray<DiagnosticInfo> Diagnostics { get; set; } = [];
}

sealed record class AggregateGenerationContext : GenerationContext
{
	public AggregateGenerationContext(Compilation compilation)
		: base(
			compilation,
			generatorName: $"{AssemblyInfo.AssemblyName}.{nameof(AggregateSourceGenerator)}",
			generatorVersion: AssemblyInfo.Version
		)
	{
		AggregateBase = compilation.GetTypeByMetadataName(
			TypeLibrary.Aggregates.AggregateBase.MetadataFullName
		);
	}

	public INamedTypeSymbol? AggregateBase { get; }
}
