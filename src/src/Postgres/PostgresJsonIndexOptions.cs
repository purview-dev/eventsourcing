using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Purview.EventSourcing.Postgres;

/// <summary>
/// Configures runtime-managed PostgreSQL indexes over a JSON payload column.
/// </summary>
/// <remarks>
/// Indexes are created when the table is auto-created and are best-effort: an index that
/// already exists (for example, created by a concurrent process) is treated as success.
/// </remarks>
public sealed class PostgresJsonIndexOptions
{
	/// <summary>
	/// When <see langword="true"/>, the configured indexes are created for the payload column.
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// When <see langword="true"/>, the GIN index uses the <c>jsonb_path_ops</c> operator class.
	/// </summary>
	/// <remarks>
	/// <c>jsonb_path_ops</c> produces a smaller index and is faster for <c>@&gt;</c> containment
	/// queries, but does not support the <c>?</c>, <c>?|</c>, and <c>?&amp;</c> operators.
	/// </remarks>
	public bool UseJsonbPathOps { get; set; }

	/// <summary>
	/// Optional name for the GIN index over the payload column; when null, a name is generated from the table name.
	/// </summary>
	public string? GinIndexName { get; set; }

	/// <summary>
	/// The path indexes to create over the payload column.
	/// </summary>
	[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "DTO")]
	public PostgresJsonPathIndexDefinition[] PathIndexes { get; set; } = [];
}

/// <summary>
/// Defines an index over a JSON path within the payload column.
/// </summary>
/// <remarks>
/// The path is expressed using JSONPath syntax, for example <c>"$.customer.name"</c>.
/// </remarks>
public sealed class PostgresJsonPathIndexDefinition
{
	/// <summary>
	/// The JSONPath expression that identifies the indexed member.
	/// </summary>
	[Required]
	public string Path { get; set; } = default!;

	/// <summary>
	/// Optional name for the index; when null, a name is generated from the table name and path.
	/// </summary>
	public string? IndexName { get; set; }
}
