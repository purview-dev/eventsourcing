using System.ComponentModel.DataAnnotations;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<PostgresServerResource>(Platform.Postgres)]
sealed partial class PostgresKit
{
	public IResourceBuilder<PostgresDatabaseResource> Database { get; private set; }
	public IResourceBuilder<PostgresDatabaseResource> SharedQueryDatabase { get; private set; }

	protected override IResourceBuilder<PostgresServerResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var postgresPassword = builder.AddParameter("postgres-password", "postgres", secret: true);
		var postgres = builder.AddPostgres(Name, postgresPassword);

		Database = postgres.AddDatabase(Platform.PostgresDatabase, Options.DatabaseName);
		SharedQueryDatabase = postgres.AddDatabase(
			Platform.PostgresSharedQueryDatabase,
			Options.SharedQueryDatabaseName
		);

		if (HostKit.Options.UseDataVolumes)
			postgres.WithDataVolume("eventsourcing-sample-postgres-data");

		return postgres;
	}

	partial class PostgresKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		public string DatabaseName { get; set; } = "event_sourcing_sample";

		[Required(AllowEmptyStrings = false)]
		public string SharedQueryDatabaseName { get; set; } = "event_sourcing_sample_azure";
	}
}
