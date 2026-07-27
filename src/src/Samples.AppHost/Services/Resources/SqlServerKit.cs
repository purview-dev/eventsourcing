using System.ComponentModel.DataAnnotations;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.Services.Resources;

[ResourceDefinition<SqlServerServerResource>]
sealed partial class SqlServerKit
{
	public IResourceBuilder<SqlServerDatabaseResource> Database { get; private set; } = default!;

	protected override IResourceBuilder<SqlServerServerResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		//var databaseName =
		//	args?.FirstOrDefault(static s => s.StartsWith("--DatabaseName=", StringComparison.OrdinalIgnoreCase))
		//		?.Split('=')
		//		.LastOrDefault()
		//	?? "EventSourcingSampleDb";

		var sqlPassword = builder.AddParameter("sql-password", "PaSsw0rd!!1!", secret: true);
		var sql = builder.AddSqlServer(Name, sqlPassword).WithImageTag(ContainerHelper.SqlServerImageTag);
		sql.AddDatabase("eventstore-db", Options.DatabaseName);

		if (!HostKit.Options.IsTestRun)
			ResourceBuilder.WithDataVolume("eventsourcing-sample-sql-data");

		return sql;
	}

	partial class SqlServerKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		public string DatabaseName { get; set; } = "EventSourcingSampleDb";
	}
}
