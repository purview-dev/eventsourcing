using System.ComponentModel.DataAnnotations;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<SqlServerServerResource>(Platform.SqlServer)]
sealed partial class SqlServerKit
{
	public IResourceBuilder<SqlServerDatabaseResource> Database { get; private set; }
	public IResourceBuilder<SqlServerDatabaseResource> SharedQueryDatabase { get; private set; }

	protected override IResourceBuilder<SqlServerServerResource> BuildResource(
		IDistributedApplicationBuilder builder
	)
	{
		var sqlPassword = builder.AddParameter("sql-password", "PaSsw0rd!!1!", secret: true);
		var sql = builder
			.AddSqlServer(Name, sqlPassword)
			.WithImageTag(ContainerHelper.SqlServerImageTag);

		Database = sql.AddDatabase(Platform.SqlDatabase, Options.DatabaseName);
		SharedQueryDatabase = sql.AddDatabase(
			Platform.SqlSharedQueryDatabase,
			Options.SharedQueryDatabaseName
		);

		if (HostKit.Options.UseDataVolumes)
			sql.WithDataVolume("eventsourcing-sample-sql-data");

		return sql;
	}

	partial class SqlServerKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		[RegularExpression(@"^[\w\-.]+$")]
		public string DatabaseName { get; set; } = "EventSourcingSampleDb";

		[Required(AllowEmptyStrings = false)]
		[RegularExpression(@"^[\w\-.]+$")]
		public string SharedQueryDatabaseName { get; set; } = "EventSourcingSampleAzureSqlDb";
	}
}
