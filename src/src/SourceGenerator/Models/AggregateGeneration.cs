using Microsoft.CodeAnalysis;

using Purview.EventSourcing.SourceGenerator.Helpers;

using System.Collections.Immutable;

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
		: base(compilation)
	{
		AggregateBase = compilation.GetTypeByMetadataName(TypeLibrary.Aggregates.AggregateBase);
	}

	public INamedTypeSymbol? AggregateBase { get; }
}
