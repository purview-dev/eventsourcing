using Purview.EventSourcing.Postgres.Events;

namespace Purview.EventSourcing.Admin.Postgres.Internal;

static class PostgresAdminTableResolver
{
	public static IReadOnlyList<PostgresAdminTableDescriptor> ResolveTables(
		PostgresEventStoreOptions options,
		string? aggregateType
	)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (!string.IsNullOrWhiteSpace(aggregateType))
			return [ResolveTable(options, aggregateType)];

		List<PostgresAdminTableDescriptor> tables = [new(null, options.SchemaName, options.TableName)];

		foreach (var overrideEntry in options.AggregateTableOverrides)
		{
			if (overrideEntry.Value is null)
				continue;

			tables.Add(
				new PostgresAdminTableDescriptor(
					overrideEntry.Key,
					overrideEntry.Value.SchemaName ?? options.SchemaName,
					overrideEntry.Value.TableName ?? options.TableName
				)
			);
		}

		return tables;
	}

	public static PostgresAdminTableDescriptor ResolveTable(PostgresEventStoreOptions options, string aggregateType)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);

		if (!options.AggregateTableOverrides.TryGetValue(aggregateType, out var overrideEntry) || overrideEntry is null)
			return new PostgresAdminTableDescriptor(aggregateType, options.SchemaName, options.TableName);

		return new PostgresAdminTableDescriptor(
			aggregateType,
			overrideEntry.SchemaName ?? options.SchemaName,
			overrideEntry.TableName ?? options.TableName
		);
	}
}
