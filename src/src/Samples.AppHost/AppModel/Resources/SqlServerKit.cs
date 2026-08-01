using System.ComponentModel.DataAnnotations;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<SqlServerServerResource>(Platform.SqlServer)]
sealed partial class SqlServerKit
{
	public IResourceBuilder<SqlServerDatabaseResource> Database { get; private set; } = default!;
	public IResourceBuilder<SqlServerDatabaseResource> SharedQueryDatabase { get; private set; } = default!;

	protected override IResourceBuilder<SqlServerServerResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var sqlPassword = builder.AddParameter("sql-password", "PaSsw0rd!!1!", secret: true);
		var sql = builder.AddSqlServer(Name, sqlPassword).WithImageTag(ContainerHelper.SqlServerImageTag);

		Database = sql.AddDatabase(Platform.SqlDatabase, Options.DatabaseName);
		SharedQueryDatabase = sql.AddDatabase(Platform.SqlSharedQueryDatabase, Options.SharedQueryDatabaseName);

		if (!HostKit.Options.IsTestRun)
			sql.WithDataVolume("eventsourcing-sample-sql-data");

		return sql;
	}

	partial class SqlServerKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		public string DatabaseName { get; set; } = "EventSourcingSampleDb";

		[Required(AllowEmptyStrings = false)]
		public string SharedQueryDatabaseName { get; set; } = "EventSourcingSampleAzureSqlDb";
	}
}
