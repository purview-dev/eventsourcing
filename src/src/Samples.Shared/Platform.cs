namespace Purview.EventSourcing.Samples;

public static class Platform
{
	public const string AspireC4 = "aspirec4";

	public const string SqlServer = "eventstore-sql";
	public const string SqlDatabase = "eventstore-db";
	public const string SqlSharedQueryDatabase = "eventstore-shared-db";

	public const string Postgres = "eventstore-postgres";
	public const string PostgresDatabase = "eventstore-postgres-db";
	public const string PostgresSharedQueryDatabase = "eventstore-postgres-shared-db";

	public const string MongoDb = "eventstore-mongo";
	public const string MongoDatabase = "eventstore-mongo-db";
	public const string MongoSharedQueryDatabase = "eventstore-mongo-shared-db";

	public const string Redis = "redis";

	public const string AzureStorage = "storage";
	public const string AzureStorageBlob = "eventstore-snapshots";

	public const string WebApp = "sample-web-app";
	public const string SqlWebApp = "sample-web-sql";
	public const string PostgresWebApp = "sample-web-postgres";
	public const string MongoDbWebApp = "sample-web-mongo";
	public const string AzureSqlWebApp = "sample-web-azure-sql";
	public const string AzurePostgresWebApp = "sample-web-azure-postgres";
	public const string AzureMongoDbWebApp = "sample-web-azure-mongo";
}
