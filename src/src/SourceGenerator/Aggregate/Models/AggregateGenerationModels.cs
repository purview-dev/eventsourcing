namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

sealed record AggregateGenerationCapabilities(bool HasAggregateBase) : IGenerationCapabilities;

// Recreated per source output callback; it never owns a CodeWriter, which is created
// and threaded explicitly through the emitters within each output callback.
sealed class AggregateEmitContext(
	GenerationContext<AggregateGenerationCapabilities> generationContext,
	AggregateInfo aggregate
) : ISourceGenLogger
{
	public GenerationContext<AggregateGenerationCapabilities> Generation { get; } = generationContext;

	public AggregateInfo Aggregate { get; } = aggregate;

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
		Generation.Log(level, indentation, message, args);
}
