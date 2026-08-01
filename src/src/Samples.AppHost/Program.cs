using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Samples;
using Purview.EventSourcing.Samples.AppHost.AppModel;
using Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

var builder = DistributedApplication.CreateBuilder(args);

if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Samples.AppHost v{AssemblyInfo.Version}";

builder
	.Services.AddOptions<SampleAppHostKit.SampleAppHostKitOptions>()
	.BindConfiguration(SampleAppHostKit.SampleAppHostKitOptions.SectionName)
	.ValidateOnStart();

var hostKitOptions =
	builder
		.Configuration.GetSection(SampleAppHostKit.SampleAppHostKitOptions.SectionName)
		.Get<SampleAppHostKit.SampleAppHostKitOptions>()
	?? new();
var hostKit = new SampleAppHostKit(hostKitOptions);
hostKit.Build(builder);
hostKit.Configure();
builder.Services.AddSingleton(hostKit);

var variants = new[]
{
	CreateVariant(
		new(
			Key: "sql-server",
			DisplayName: "SQL Server",
			Description: "SQL Server event store with SQL Server query snapshots and admin API.",
			EventStore: SampleEventStoreKind.SqlServer,
			QueryStore: SampleQueryStoreKind.SqlServer,
			AdminStore: SampleAdminStoreKind.SqlServer,
			AdminApiAvailable: true,
			EventStoreConnectionName: Platform.SqlDatabase,
			QueryStoreConnectionName: Platform.SqlDatabase
		),
		Platform.WebApp,
		project => project.WithReference(hostKit.SqlServer.Database).WaitFor(hostKit.SqlServer.Database)
	),
	CreateVariant(
		new(
			Key: "postgres",
			DisplayName: "Postgres",
			Description: "Postgres event store with Postgres query snapshots and admin API.",
			EventStore: SampleEventStoreKind.Postgres,
			QueryStore: SampleQueryStoreKind.Postgres,
			AdminStore: SampleAdminStoreKind.Postgres,
			AdminApiAvailable: true,
			EventStoreConnectionName: Platform.PostgresDatabase,
			QueryStoreConnectionName: Platform.PostgresDatabase
		),
		Platform.PostgresWebApp,
		project => project.WithReference(hostKit.Postgres.Database).WaitFor(hostKit.Postgres.Database)
	),
	CreateVariant(
		new(
			Key: "mongo-db",
			DisplayName: "MongoDB",
			Description: "MongoDB event store with MongoDB query snapshots and admin API.",
			EventStore: SampleEventStoreKind.MongoDb,
			QueryStore: SampleQueryStoreKind.MongoDb,
			AdminStore: SampleAdminStoreKind.MongoDb,
			AdminApiAvailable: true,
			EventStoreConnectionName: Platform.MongoDb,
			QueryStoreConnectionName: Platform.MongoDb,
			EventStoreDatabaseName: Platform.MongoDatabase,
			QueryStoreDatabaseName: Platform.MongoDatabase,
			AdminDatabaseName: Platform.MongoDatabase
		),
		Platform.MongoDbWebApp,
		project => project.WithReference(hostKit.MongoDb).WaitFor(hostKit.MongoDb.Database)
	),
	CreateVariant(
		new(
			Key: "azure-storage-sql",
			DisplayName: "Azure Storage + SQL Server query store",
			Description: "Azure Storage append-only event store with SQL Server query snapshots in a dedicated shared database.",
			EventStore: SampleEventStoreKind.AzureStorage,
			QueryStore: SampleQueryStoreKind.SqlServer,
			AdminStore: SampleAdminStoreKind.AzureStorage,
			AdminApiAvailable: true,
			EventStoreConnectionName: Platform.AzureStorageBlob,
			QueryStoreConnectionName: Platform.SqlSharedQueryDatabase
		),
		Platform.AzureSqlWebApp,
		project =>
		{
			project.WithReference(hostKit.SqlServer.SharedQueryDatabase).WaitFor(hostKit.SqlServer.SharedQueryDatabase);
		}
	),
	CreateVariant(
		new(
			Key: "azure-storage-postgres",
			DisplayName: "Azure Storage + Postgres query store",
			Description: "Azure Storage append-only event store with Postgres query snapshots in a dedicated shared database.",
			EventStore: SampleEventStoreKind.AzureStorage,
			QueryStore: SampleQueryStoreKind.Postgres,
			AdminStore: SampleAdminStoreKind.AzureStorage,
			AdminApiAvailable: true,
			EventStoreConnectionName: Platform.AzureStorageBlob,
			QueryStoreConnectionName: Platform.PostgresSharedQueryDatabase
		),
		Platform.AzurePostgresWebApp,
		project =>
		{
			project.WithReference(hostKit.Postgres.SharedQueryDatabase).WaitFor(hostKit.Postgres.SharedQueryDatabase);
		}
	),
	CreateVariant(
		new(
			Key: "azure-storage-mongo",
			DisplayName: "Azure Storage + MongoDB query store",
			Description: "Azure Storage append-only event store with MongoDB query snapshots in a dedicated shared database.",
			EventStore: SampleEventStoreKind.AzureStorage,
			QueryStore: SampleQueryStoreKind.MongoDb,
			AdminStore: SampleAdminStoreKind.AzureStorage,
			AdminApiAvailable: true,
			EventStoreConnectionName: Platform.AzureStorageBlob,
			QueryStoreConnectionName: Platform.MongoDb,
			QueryStoreDatabaseName: Platform.MongoSharedQueryDatabase
		),
		Platform.AzureMongoDbWebApp,
		project =>
		{
			project.WithReference(hostKit.MongoDb).WaitFor(hostKit.MongoDb.SharedQueryDatabase);
		}
	),
};

SampleWebProjectResources.AddVariantLinks(variants);

var app = builder.Build();

await app.RunAsync();

SampleWebProjectVariant CreateVariant(
	SampleWebProjectConfiguration configuration,
	string resourceName,
	Action<IResourceBuilder<ProjectResource>> configureReferences
)
{
	var resource = SampleWebProjectResources.AddSampleWebProject(
		builder,
		hostKit,
		resourceName,
		configuration,
		configureReferences
	);
	return new SampleWebProjectVariant(configuration, resource);
}
