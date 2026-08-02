using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Purview.EventSourcing.Samples;

public enum SampleEventStoreKind
{
	SqlServer,
	Postgres,
	MongoDb,
	AzureStorage,
}

public enum SampleQueryStoreKind
{
	SqlServer,
	Postgres,
	MongoDb,
}

public enum SampleAdminStoreKind
{
	None,
	SqlServer,
	Postgres,
	MongoDb,
	AzureStorage,
}

public sealed class SampleStoreOptions
{
	public const string SectionName = "SampleStore";

	public string CurrentKey { get; set; } = "sql-server";

	public string CurrentDisplayName { get; set; } = "SQL Server";

	public string CurrentDescription { get; set; } = "SQL Server event streams with SQL Server query snapshots.";

	public string DataIsolationWarning { get; set; } =
		"Each sample option uses its own backing data. Switching providers shows a different seeded dataset.";

	public SampleEventStoreKind EventStore { get; set; } = SampleEventStoreKind.SqlServer;

	public SampleQueryStoreKind QueryStore { get; set; } = SampleQueryStoreKind.SqlServer;

	public SampleAdminStoreKind AdminStore { get; set; } = SampleAdminStoreKind.SqlServer;

	public bool AdminAPIAvailable { get; set; } = true;

	public string EventStoreConnectionName { get; set; } = Platform.SqlDatabase;

	public string QueryStoreConnectionName { get; set; } = Platform.SqlDatabase;

	[RegularExpression(@"^[\w\-.]+$")]
	public string? EventStoreDatabaseName { get; set; }

	[RegularExpression(@"^[\w\-.]+$")]
	public string? QueryStoreDatabaseName { get; set; }

	[RegularExpression(@"^[\w\-.]+$")]
	public string? AdminDatabaseName { get; set; }

	public string AdminSitePath { get; set; } = "/admin";

	public string AdminAPIPath { get; set; } = "/admin/api";

	public Collection<SampleStoreVariantLink> Variants { get; } = [];
}

public sealed class SampleStoreVariantLink
{
	public string Key { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
	public string Url { get; set; } = string.Empty;
}
