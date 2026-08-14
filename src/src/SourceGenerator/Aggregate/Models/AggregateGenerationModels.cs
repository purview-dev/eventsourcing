using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

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
	public AggregateGenerationContext(
		Compilation compilation,
		string generatorName,
		string generatorVersion,
		bool validateCodeWriterScopes
	)
		: base(compilation, generatorName, generatorVersion, validateCodeWriterScopes)
	{
		AggregateBase = compilation.GetTypeByMetadataName(
			TypeLibrary.Aggregates.AggregateBase.MetadataFullName
		);
	}

	public INamedTypeSymbol? AggregateBase { get; }
}
