using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

sealed record AggregateGenerationModel(
	AggregateGenerationContext GenerationContext,
	EquatableArray<GeneratorResult<AggregateInfo>> Aggregates,
	EquatableArray<DiagnosticInfo> Diagnostics
);

sealed class AggregateGenerationContext(
	Compilation compilation,
	GenerationSettings generationSettings,
	ISourceGenLogger? logger
) : GenerationContext(compilation, generationSettings, logger)
{
	public INamedTypeSymbol? AggregateBase { get; } =
		compilation.GetTypeByMetadataName(TypeLibrary.Aggregates.AggregateBase.MetadataFullName);

	public CodeWriter Writer { get; private set; }
}

// This is recreated outside of the pipeline to avoid the state
// of the CodeWriter being shared across multiple source outputs.
sealed class AggregateGenerationOutputContext(
	AggregateGenerationContext generationContext,
	AggregateInfo aggregate
) : ISourceGenLogger
{
	public AggregateGenerationContext Generation { get; } = generationContext;

	public AggregateInfo Aggregate { get; } = aggregate;

	public CodeWriter Writer { get; private set; } = generationContext.CreateCodeWriter();

	public CodeWriter CreateCodeWriter() => Writer = Generation.CreateCodeWriter();

	public void Log(
		SourceGenLogLevel level,
		int indentation,
		string message,
		params object[] args
	) => Generation.Log(level, indentation, message, args);
}
