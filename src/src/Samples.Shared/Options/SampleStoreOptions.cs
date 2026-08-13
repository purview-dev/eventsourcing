using System.ComponentModel.DataAnnotations;
using ZodSharp;

namespace Purview.EventSourcing.Samples.Options;

[ZodSchema]
public sealed class SampleStoreOptions
{
	public const string SectionName = "SampleStore";

	[Required(AllowEmptyStrings = false)]
	public string CurrentKey { get; set; } = "sql-server";

	[Required(AllowEmptyStrings = false)]
	public string CurrentDisplayName { get; set; } = "SQL Server";

	[Required(AllowEmptyStrings = false)]
	public string CurrentDescription { get; set; } =
		"SQL Server event streams with SQL Server query snapshots.";

	[Required(AllowEmptyStrings = false)]
	public string DataIsolationWarning { get; set; } =
		"Each sample option uses its own backing data. Switching providers shows a different seeded dataset.";

	public SampleEventStoreKind EventStore { get; set; } = SampleEventStoreKind.SqlServer;

	public SampleQueryStoreKind QueryStore { get; set; } = SampleQueryStoreKind.SqlServer;

	public SampleAdminStoreKind AdminStore { get; set; } = SampleAdminStoreKind.SqlServer;

	public bool AdminAPIAvailable { get; set; } = true;

	[Required(AllowEmptyStrings = false)]
	public string EventStoreConnectionName { get; set; } = Platform.SqlDatabase;

	[Required(AllowEmptyStrings = false)]
	public string QueryStoreConnectionName { get; set; } = Platform.SqlDatabase;

	[RegularExpression(@"^[\w\-.]+$")]
	public string? EventStoreDatabaseName { get; set; }

	[RegularExpression(@"^[\w\-.]+$")]
	public string? QueryStoreDatabaseName { get; set; }

	[RegularExpression(@"^[\w\-.]+$")]
	public string? AdminDatabaseName { get; set; }

	[Required(AllowEmptyStrings = false)]
	public string AdminSitePath { get; set; } = "/admin";

	[Required(AllowEmptyStrings = false)]
	public string AdminAPIPath { get; set; } = "/admin/api";
}
