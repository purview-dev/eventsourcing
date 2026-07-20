using Purview.EventSourcing.Fixtures.SqlServer;
using Purview.EventSourcing.Samples.Domain.ReportUpload;
using Purview.EventSourcing.Samples.ValueObjects;

namespace Purview.EventSourcing.SqlServer.Snapshots;

[ClassDataSource<SqlServerSnapshotEventStoreFixture>(Shared = SharedType.PerAssembly)]
public sealed class ReportUploadAggregateSnapshotEventStoreTests(SqlServerSnapshotEventStoreFixture fixture)
{
	static readonly Faker Faker = new();

	[Test]
	public async Task SnapshotAsync_GivenReportUploadAggregateWithLineItems_QueriesByLineItemCount(
		CancellationToken cancellationToken
	)
	{
		var store = fixture.CreateSnapshotStore<ReportUploadAggregate>();
		var id = Guid.NewGuid().ToString("D");

		var aggregate = TestHelpers.Aggregate<ReportUploadAggregate>(
			id,
			agg =>
				agg.Create(
						"a-file-name.json",
						"213123",
						BlobUri.Create(new Uri("/a/path/to/the/original/json", UriKind.Relative)),
						UserCapture.Create(
							UserDetails.Create(Guid.NewGuid(), "Testing Account", true),
							DateTimeOffset.UtcNow
						)
					)
					.AddExcelReport(
						GuidObjectId.Create(Guid.NewGuid()),
						BlobUri.Create(new Uri("/a/path/to/an/object", UriKind.Relative))
					)
					.MarkAsComplete(
						ReportSummary.Create(
							new()
							{
								AssetDetails = new AssetDetails(
									new Dictionary<PlatformID, int>
									{
										{ PlatformID.Win32NT, Faker.Random.Int(1, 100) },
										{ PlatformID.Unix, Faker.Random.Int(1, 100) },
										{ PlatformID.Other, Faker.Random.Int(1, 100) },
									}
								),
								ParserDetails = new(10, 5, 5, TimeSpan.FromMinutes(1)),
								Projects = Faker.Make(
									2,
									i => new Project($"Project {i + 1}", $"{i + 1}", $"Team {i + 1}")
								),
								VulnerabilityDetails = new VulnerabilityDetails(100, 10, 10, 20, 30, 40),
							}
						)
					),
			clearEvents: false
		);

		await store.SaveAsync(aggregate, cancellationToken);
	}
}
