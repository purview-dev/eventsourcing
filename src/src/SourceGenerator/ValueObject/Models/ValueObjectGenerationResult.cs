using System.Collections.Immutable;

namespace Purview.EventSourcing.SourceGenerator.ValueObject.Models;

readonly record struct ValueObjectGenerationResult(
	string? HintName,
	string? Source,
	ImmutableArray<DiagnosticInfo> Diagnostics
)
{
	public static ValueObjectGenerationResult Empty { get; } = new(null, null, []);
}
