using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Purview.EventSourcing.SqlServer;

/// <summary>
/// Configures runtime-managed SQL Server indexes over JSON payload columns.
/// </summary>
public sealed class SqlServerJsonIndexOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether configured JSON indexes should be created
	/// when <c>AutoCreateTable</c> is enabled for the store.
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// Gets or sets the JSON indexes to create for the payload column.
	/// </summary>
	[SuppressMessage(
		"Performance",
		"CA1819:Properties should not return arrays",
		Justification = "DTO"
	)]
	public SqlServerJsonIndexDefinition[] Indexes { get; set; } = [];
}

/// <summary>
/// Defines a single SQL Server index over a JSON payload path.
/// </summary>
public sealed class SqlServerJsonIndexDefinition
{
	/// <summary>
	/// Gets or sets the JSON path to extract from the payload column.
	/// Example: <c>$.Status</c> or <c>$.Customer.Name</c>.
	/// </summary>
	[Required]
	public string JsonPath { get; set; } = default!;

	/// <summary>
	/// Gets or sets the optional SQL index name. When omitted, a deterministic name is generated.
	/// </summary>
	public string? IndexName { get; set; }

	/// <summary>
	/// Gets or sets the optional computed-column name used to materialize the JSON path.
	/// When omitted, a deterministic name is generated.
	/// </summary>
	public string? ComputedColumnName { get; set; }

	/// <summary>
	/// Gets or sets the SQL type used when casting the extracted JSON value.
	/// </summary>
	[Required]
	public string SqlType { get; set; } = "nvarchar(450)";

	/// <summary>
	/// Gets or sets a value indicating whether the generated index should be unique.
	/// </summary>
	public bool Unique { get; set; }

	/// <summary>
	/// Gets or sets the computed-column persistence mode.
	/// </summary>
	[EnumDataType(typeof(SqlServerJsonComputedColumnMode))]
	public SqlServerJsonComputedColumnMode ComputedColumnMode { get; set; } =
		SqlServerJsonComputedColumnMode.Persisted;

	/// <summary>
	/// Gets or sets the optional include columns for the generated index.
	/// </summary>
	[SuppressMessage(
		"Performance",
		"CA1819:Properties should not return arrays",
		Justification = "DTO"
	)]
	public string[] IncludeColumns { get; set; } = [];

	/// <summary>
	/// Gets or sets the optional SQL filter clause for the generated index.
	/// </summary>
	public string? Filter { get; set; }
}

/// <summary>
/// Controls whether generated JSON computed columns are persisted.
/// </summary>
public enum SqlServerJsonComputedColumnMode
{
	/// <summary>
	/// Creates a non-persisted computed column.
	/// </summary>
	NonPersisted = 0,

	/// <summary>
	/// Creates a persisted computed column.
	/// </summary>
	Persisted = 1,
}
