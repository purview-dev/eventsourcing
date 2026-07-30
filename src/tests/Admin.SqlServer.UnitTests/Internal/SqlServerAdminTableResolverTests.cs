using Purview.EventSourcing.SqlServer.Events;
using TUnit.Core;

namespace Purview.EventSourcing.Admin.SqlServer.UnitTests.Internal;

public sealed class SqlServerAdminTableResolverTests
{
	[Test]
	public async Task ResolveTables_ReturnsDefaultTable_WhenAggregateTypeIsNotSpecified()
	{
		var options = new SqlServerEventStoreOptions
		{
			ConnectionString = "Server=.;Database=Db;Trusted_Connection=True;",
			SchemaName = "dbo",
			TableName = "EventStoreEvents",
		};

		var tables = SqlServerAdminTableResolver.ResolveTables(options, null);

		if (tables.Count != 1)
			throw new InvalidOperationException($"Expected 1 table, got {tables.Count}");

		if (tables[0].SchemaName != "dbo")
			throw new InvalidOperationException($"Expected dbo schema, got {tables[0].SchemaName}");

		if (tables[0].TableName != "EventStoreEvents")
			throw new InvalidOperationException($"Expected EventStoreEvents table, got {tables[0].TableName}");
	}

	[Test]
	public async Task ResolveTable_UsesAggregateOverride_WhenConfigured()
	{
		var options = new SqlServerEventStoreOptions
		{
			ConnectionString = "Server=.;Database=Db;Trusted_Connection=True;",
			SchemaName = "dbo",
			TableName = "EventStoreEvents",
			AggregateTableOverrides =
			{
				["Order"] = new SqlServerAggregateTableOverride { SchemaName = "orders", TableName = "OrderEvents" },
			},
		};

		var table = SqlServerAdminTableResolver.ResolveTable(options, "Order");

		if (table.AggregateTypeFilter != "Order")
			throw new InvalidOperationException($"Expected Order filter, got {table.AggregateTypeFilter}");

		if (table.SchemaName != "orders")
			throw new InvalidOperationException($"Expected orders schema, got {table.SchemaName}");

		if (table.TableName != "OrderEvents")
			throw new InvalidOperationException($"Expected OrderEvents table, got {table.TableName}");
	}
}
