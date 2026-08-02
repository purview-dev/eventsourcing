using Purview.EventSourcing.Samples.Fixtures;

namespace Purview.EventSourcing.Samples.Web.Infrastructure;

[ClassDataSource<AppHostFixture>(Shared = SharedType.PerTestSession)]
public sealed class AzureStorageConnectionStringTests(AppHostFixture fixture)
{
	[Test]
	public async Task AzureTableResourceConnection_ContainsTableEndpoint(CancellationToken cancellationToken)
	{
		var connectionString = await fixture.GetResourceConnectionStringAsync(
			Platform.AzureStorageTable,
			cancellationToken
		);

		await Assert.That(connectionString).IsNotNull();
		await Assert.That(connectionString!).Contains("TableEndpoint=");
	}

	[Test]
	public async Task AzureBlobResourceConnection_ContainsBlobEndpoint(CancellationToken cancellationToken)
	{
		var connectionString = await fixture.GetResourceConnectionStringAsync(
			Platform.AzureStorageBlob,
			cancellationToken
		);

		await Assert.That(connectionString).IsNotNull();
		await Assert.That(connectionString!).Contains("BlobEndpoint=");
	}
}
