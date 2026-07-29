namespace Purview.EventSourcing.Samples;

public static class Platform
{
	public const string AspireC4 = "aspirec4";

	public const string SqlServer = "eventstore-sql";
	public const string SqlDatabase = "eventstore-db";

	public const string Redis = "redis";

	public const string AzureStorage = "storage";
	public const string AzureStorageBlob = "eventstore-snapshots";

	public const string WebApp = "sample-web-app";
}
