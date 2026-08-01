using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Purview.EventSourcing.Postgres;

public sealed class PostgresJsonIndexOptions
{
	public bool Enabled { get; set; } = true;

	public bool UseJsonbPathOps { get; set; }

	public string? GinIndexName { get; set; }

	[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "DTO")]
	public PostgresJsonPathIndexDefinition[] PathIndexes { get; set; } = [];
}

public sealed class PostgresJsonPathIndexDefinition
{
	[Required]
	public string Path { get; set; } = default!;

	public string? IndexName { get; set; }
}
