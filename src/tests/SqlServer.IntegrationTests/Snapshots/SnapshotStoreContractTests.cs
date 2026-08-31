using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.Contracts;
using Purview.EventSourcing.Fixtures.SqlServer;
using Purview.EventSourcing.SqlServer.Snapshot;

namespace Purview.EventSourcing.SqlServer.Snapshots;

[GenerateGenericTest(typeof(PersistenceAggregate))]
[ClassDataSource<SqlServerSnapshotEventStoreFixture>(Shared = SharedType.PerTestSession)]
[InheritsTests]
public sealed class SnapshotStoreContractTests<TAggregate>(SqlServerSnapshotEventStoreFixture fixture)
	: SnapshotStoreContractTestsBase<TAggregate>
	where TAggregate : class, IAggregateTest, new()
{
	protected override IQueryableEventStoreCore<TAggregate> CreateSnapshotStore() =>
		fixture.CreateSnapshotStore<TAggregate>();

	protected override Task SnapshotAsync(
		IQueryableEventStoreCore<TAggregate> store,
		TAggregate aggregate,
		CancellationToken cancellationToken
	) => ((SqlServerSnapshotEventStore<TAggregate>)store).SnapshotAsync(aggregate, cancellationToken);
}
