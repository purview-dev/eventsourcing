namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

sealed record AggregateGenerationModel(
	GenerationContext<AggregateGenerationCapabilities> GenerationContext,
	EquatableArray<GeneratorResult<AggregateInfo>> Aggregates,
	EquatableArray<DiagnosticInfo> Diagnostics
);

sealed record AggregateGenerationCapabilities(bool HasAggregateBase) : IGenerationCapabilities;

// This is recreated outside of the pipeline to avoid the state
// of the CodeWriter being shared across multiple source outputs.
sealed class AggregateEmitContext(
	GenerationContext<AggregateGenerationCapabilities> generationContext,
	AggregateInfo aggregate
) : ISourceGenLogger
{
	public GenerationContext<AggregateGenerationCapabilities> Generation { get; } = generationContext;

	public AggregateInfo Aggregate { get; } = aggregate;

	public CodeWriter Writer { get; private set; } = generationContext.CreateCodeWriter();

	public CodeWriter CreateCodeWriter() => Writer = Generation.CreateCodeWriter();

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
		Generation.Log(level, indentation, message, args);

	public AggregateEmitContext WithWriter(CodeWriter writer)
	{
		Writer = writer;
		return this;
	}
}
