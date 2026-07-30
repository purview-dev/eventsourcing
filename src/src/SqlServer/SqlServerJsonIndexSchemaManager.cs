using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Purview.EventSourcing.SqlServer;

static partial class SqlServerJsonIndexSchemaManager
{
	public static void ValidateOrThrow(
		SqlServerJsonIndexOptions? options,
		string schemaName,
		string tableName,
		IReadOnlySet<string> supportedIncludeColumns,
		string optionsName
	)
	{
		_ = CreateDescriptors(options, schemaName, tableName, supportedIncludeColumns, optionsName);
	}

	public static async Task ApplyAsync(
		SqlConnection connection,
		SqlTransaction? transaction,
		string schemaName,
		string tableName,
		SqlServerJsonIndexOptions? options,
		IReadOnlySet<string> supportedIncludeColumns,
		CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(connection);

		var descriptors = CreateDescriptors(
			options,
			schemaName,
			tableName,
			supportedIncludeColumns,
			optionsName: "JsonIndexOptions"
		);
		if (descriptors.Count == 0)
			return;

		foreach (
			var computedColumn in descriptors
				.Select(static x => x.ComputedColumn)
				.DistinctBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
		)
		{
			if (
				await ColumnExistsAsync(
					connection,
					transaction,
					schemaName,
					tableName,
					computedColumn.Name,
					cancellationToken
				)
			)
				continue;

			await ExecuteNonQueryIgnoringDuplicateSchemaAsync(
				connection,
				transaction,
				$"ALTER TABLE {QuoteTableName(schemaName, tableName)} ADD {QuoteIdentifier(computedColumn.Name)} AS ({computedColumn.Expression}){(computedColumn.Persisted ? " PERSISTED" : string.Empty)};",
				cancellationToken
			);
		}

		foreach (var descriptor in descriptors)
		{
			if (
				await IndexExistsAsync(
					connection,
					transaction,
					schemaName,
					tableName,
					descriptor.IndexName,
					cancellationToken
				)
			)
				continue;

			var includeClause =
				descriptor.IncludeColumns.Count == 0
					? string.Empty
					: $" INCLUDE ({string.Join(", ", descriptor.IncludeColumns.Select(QuoteIdentifier))})";
			var filterClause = string.IsNullOrWhiteSpace(descriptor.Filter)
				? string.Empty
				: $" WHERE {descriptor.Filter}";

			await ExecuteNonQueryIgnoringDuplicateSchemaAsync(
				connection,
				transaction,
				$"CREATE {(descriptor.Unique ? "UNIQUE " : string.Empty)}INDEX {QuoteIdentifier(descriptor.IndexName)} ON {QuoteTableName(schemaName, tableName)} ({QuoteIdentifier(descriptor.ComputedColumn.Name)}){includeClause}{filterClause};",
				cancellationToken
			);
		}
	}

	static ReadOnlyCollection<SqlServerJsonIndexDescriptor> CreateDescriptors(
		SqlServerJsonIndexOptions? options,
		string schemaName,
		string tableName,
		IReadOnlySet<string> supportedIncludeColumns,
		string optionsName
	)
	{
		ValidateIdentifier(schemaName, nameof(schemaName));
		ValidateIdentifier(tableName, nameof(tableName));

		if (options is null || !options.Enabled || options.Indexes.Length == 0)
			return Array.Empty<SqlServerJsonIndexDescriptor>().AsReadOnly();

		var errors = new List<string>();
		var indexNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var computedColumns = new Dictionary<string, SqlServerJsonComputedColumnDescriptor>(
			StringComparer.OrdinalIgnoreCase
		);
		var logicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var descriptors = new List<SqlServerJsonIndexDescriptor>();

		for (var i = 0; i < options.Indexes.Length; i++)
		{
			var errorCountBeforeDefinition = errors.Count;
			var definition = options.Indexes[i];
			if (definition is null)
			{
				errors.Add($"{optionsName}.Indexes[{i}] cannot be null.");
				continue;
			}

			var jsonPath = definition.JsonPath?.Trim();
			if (string.IsNullOrWhiteSpace(jsonPath) || !jsonPath.StartsWith('$'))
				errors.Add($"{optionsName}.Indexes[{i}].JsonPath must start with '$'.");

			var sqlType = definition.SqlType?.Trim();
			if (string.IsNullOrWhiteSpace(sqlType) || !SqlTypeRegex().IsMatch(sqlType))
				errors.Add($"{optionsName}.Indexes[{i}].SqlType '{definition.SqlType}' is not supported.");

			var filter = string.IsNullOrWhiteSpace(definition.Filter) ? null : definition.Filter.Trim();
			if (filter is not null && !IsSafeFilter(filter))
				errors.Add($"{optionsName}.Indexes[{i}].Filter contains unsupported SQL text.");

			var normalizedIncludeColumns = new List<string>();
			var seenIncludeColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var includeColumn in definition.IncludeColumns ?? [])
			{
				if (string.IsNullOrWhiteSpace(includeColumn))
				{
					errors.Add($"{optionsName}.Indexes[{i}].IncludeColumns cannot contain empty values.");
					continue;
				}

				var normalizedColumn = includeColumn.Trim();
				if (!seenIncludeColumns.Add(normalizedColumn))
				{
					errors.Add(
						$"{optionsName}.Indexes[{i}].IncludeColumns contains duplicate column '{normalizedColumn}'."
					);
					continue;
				}

				ValidateIdentifier(normalizedColumn, $"{optionsName}.Indexes[{i}].IncludeColumns");
				if (!supportedIncludeColumns.Contains(normalizedColumn))
					errors.Add(
						$"{optionsName}.Indexes[{i}].IncludeColumns contains unsupported column '{normalizedColumn}'."
					);

				normalizedIncludeColumns.Add(normalizedColumn);
			}

			if (errors.Count > errorCountBeforeDefinition)
				continue;

			var computedColumnKey = $"{jsonPath}|{sqlType}|{definition.ComputedColumnMode}";
			var computedColumnName = string.IsNullOrWhiteSpace(definition.ComputedColumnName)
				? $"JX_{CreateStableHash(computedColumnKey)}"
				: definition.ComputedColumnName.Trim();
			ValidateIdentifier(computedColumnName, $"{optionsName}.Indexes[{i}].ComputedColumnName");

			var logicalIndexKey = string.Join(
				"|",
				jsonPath,
				sqlType,
				definition.ComputedColumnMode,
				definition.Unique,
				filter ?? string.Empty,
				string.Join(",", normalizedIncludeColumns)
			);
			if (!logicalKeys.Add(logicalIndexKey))
				errors.Add($"{optionsName}.Indexes[{i}] duplicates another JSON index definition.");

			var indexName = string.IsNullOrWhiteSpace(definition.IndexName)
				? $"IX_{tableName}_{CreateStableHash(logicalIndexKey)}"
				: definition.IndexName.Trim();
			ValidateIdentifier(indexName, $"{optionsName}.Indexes[{i}].IndexName");

			if (!indexNames.Add(indexName))
				errors.Add($"{optionsName}.Indexes contains duplicate index name '{indexName}'.");

			var computedColumn = new SqlServerJsonComputedColumnDescriptor(
				computedColumnName,
				$"TRY_CAST(JSON_VALUE([Payload], N'{EscapeSqlLiteral(jsonPath!)}') AS {sqlType})",
				definition.ComputedColumnMode == SqlServerJsonComputedColumnMode.Persisted
			);

			if (computedColumns.TryGetValue(computedColumn.Name, out var existingComputedColumn))
			{
				if (
					!string.Equals(
						existingComputedColumn.Expression,
						computedColumn.Expression,
						StringComparison.Ordinal
					)
					|| existingComputedColumn.Persisted != computedColumn.Persisted
				)
				{
					errors.Add(
						$"{optionsName}.Indexes contains conflicting computed-column configuration for '{computedColumn.Name}'."
					);
				}
			}
			else
			{
				computedColumns.Add(computedColumn.Name, computedColumn);
			}

			descriptors.Add(
				new SqlServerJsonIndexDescriptor(
					indexName,
					computedColumn,
					definition.Unique,
					normalizedIncludeColumns.AsReadOnly(),
					filter
				)
			);
		}

		if (errors.Count > 0)
			throw new ArgumentException(string.Join(Environment.NewLine, errors), optionsName);

		return descriptors.AsReadOnly();
	}

	static async Task<bool> ColumnExistsAsync(
		SqlConnection connection,
		SqlTransaction? transaction,
		string schemaName,
		string tableName,
		string columnName,
		CancellationToken cancellationToken
	)
	{
		await using var command = new SqlCommand(
			"""
			SELECT COUNT(1)
			FROM sys.columns c
			INNER JOIN sys.tables t ON t.object_id = c.object_id
			INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
			WHERE s.name = @schemaName AND t.name = @tableName AND c.name = @columnName;
			""",
			connection,
			transaction
		);
		command.Parameters.AddWithValue("@schemaName", schemaName);
		command.Parameters.AddWithValue("@tableName", tableName);
		command.Parameters.AddWithValue("@columnName", columnName);
		var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
		return count > 0;
	}

	static async Task<bool> IndexExistsAsync(
		SqlConnection connection,
		SqlTransaction? transaction,
		string schemaName,
		string tableName,
		string indexName,
		CancellationToken cancellationToken
	)
	{
		await using var command = new SqlCommand(
			"""
			SELECT COUNT(1)
			FROM sys.indexes i
			INNER JOIN sys.tables t ON t.object_id = i.object_id
			INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
			WHERE s.name = @schemaName AND t.name = @tableName AND i.name = @indexName;
			""",
			connection,
			transaction
		);
		command.Parameters.AddWithValue("@schemaName", schemaName);
		command.Parameters.AddWithValue("@tableName", tableName);
		command.Parameters.AddWithValue("@indexName", indexName);
		var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
		return count > 0;
	}

	static async Task ExecuteNonQueryIgnoringDuplicateSchemaAsync(
		SqlConnection connection,
		SqlTransaction? transaction,
		string sql,
		CancellationToken cancellationToken
	)
	{
		try
		{
#pragma warning disable CA2100
			await using var command = new SqlCommand(sql, connection, transaction);
#pragma warning restore CA2100
			await command.ExecuteNonQueryAsync(cancellationToken);
		}
		catch (SqlException ex) when (ex.Number is 1911 or 1913 or 2705)
		{
			// Cross-process duplicate create race; treat as success.
		}
	}

	static string QuoteTableName(string schemaName, string tableName) =>
		$"{QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)}";

	static string QuoteIdentifier(string identifier) => $"[{identifier}]";

	static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

	static string CreateStableHash(string value)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
		return Convert.ToHexString(hash[..6]);
	}

	static bool IsSafeFilter(string filter) =>
		!filter.Contains(';', StringComparison.Ordinal)
		&& !filter.Contains("--", StringComparison.Ordinal)
		&& !filter.Contains("/*", StringComparison.Ordinal)
		&& !filter.Contains("*/", StringComparison.Ordinal)
		&& FilterRegex().IsMatch(filter);

	static void ValidateIdentifier(string identifier, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(identifier))
			throw new ArgumentException("Identifier cannot be null or empty.", parameterName);

		if (!IdentifierRegex().IsMatch(identifier))
			throw new ArgumentException($"Identifier '{identifier}' contains invalid characters.", parameterName);
	}

	[GeneratedRegex(@"^[\w\-\.]+$")]
	private static partial Regex IdentifierRegex();

	[GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*(?:\s*\(\s*\d+\s*(?:,\s*\d+\s*)?\))?$")]
	private static partial Regex SqlTypeRegex();

	[GeneratedRegex(@"^[\[\]\w\s\=\<\>\!\(\)'\.,-]+$")]
	private static partial Regex FilterRegex();

	sealed record SqlServerJsonComputedColumnDescriptor(string Name, string Expression, bool Persisted);

	sealed record SqlServerJsonIndexDescriptor(
		string IndexName,
		SqlServerJsonComputedColumnDescriptor ComputedColumn,
		bool Unique,
		ReadOnlyCollection<string> IncludeColumns,
		string? Filter
	);
}
