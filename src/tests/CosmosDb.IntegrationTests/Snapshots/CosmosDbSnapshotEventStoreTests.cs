using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.Fixtures.CosmosDb;

namespace Purview.EventSourcing.CosmosDb.Snapshots;

// Holds the fixture and shared helpers for Cosmos-specific snapshot tests (dictionary/partition-key
// queries). The provider-agnostic snapshot contract suite lives in SnapshotStoreContractTests.
[ClassDataSource<CosmosDbSnapshotEventStoreFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel(nameof(CosmosDbClient))]
public partial class CosmosDbSnapshotEventStoreTests(CosmosDbSnapshotEventStoreFixture fixture)
{
	static PersistenceAggregate CreateAggregate(string? id = null, Action<PersistenceAggregate>? action = null)
	{
		PersistenceAggregate aggregate = new() { Details = { Id = id ?? Guid.NewGuid().ToString() } };

		action?.Invoke(aggregate);

		return aggregate;
	}
}
