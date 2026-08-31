using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.Contracts;
using Purview.EventSourcing.Fixtures.Postgres;
using Purview.EventSourcing.Postgres.Snapshot;

namespace Purview.EventSourcing.Postgres.Snapshots;

[GenerateGenericTest(typeof(PersistenceAggregate))]
[ClassDataSource<PostgresSnapshotEventStoreFixture>(Shared = SharedType.PerTestSession)]
[InheritsTests]
public sealed class SnapshotStoreContractTests<TAggregate>(PostgresSnapshotEventStoreFixture fixture)
	: SnapshotStoreContractTestsBase<TAggregate>
	where TAggregate : class, IAggregateTest, new()
{
	protected override IQueryableEventStoreCore<TAggregate> CreateSnapshotStore() =>
		fixture.CreateSnapshotStore<TAggregate>();

	protected override Task SnapshotAsync(
		IQueryableEventStoreCore<TAggregate> store,
		TAggregate aggregate,
		CancellationToken cancellationToken
	) => ((PostgresSnapshotEventStore<TAggregate>)store).SnapshotAsync(aggregate, cancellationToken);
}
