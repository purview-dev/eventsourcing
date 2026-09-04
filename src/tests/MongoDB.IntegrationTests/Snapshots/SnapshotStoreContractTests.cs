using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.Contracts;
using Purview.EventSourcing.Fixtures.MongoDB;

namespace Purview.EventSourcing.MongoDB.Snapshots;

[GenerateGenericTest(typeof(PersistenceAggregate))]
[ClassDataSource<MongoDBSnapshotEventStoreFixture>(Shared = SharedType.PerTestSession)]
[InheritsTests]
public sealed class SnapshotStoreContractTests<TAggregate>(MongoDBSnapshotEventStoreFixture fixture)
	: SnapshotStoreContractTestsBase<TAggregate>
	where TAggregate : class, IAggregateTest, new()
{
	MongoDBSnapshotTestContext? _context;

	protected override IQueryableEventStoreCore<TAggregate> CreateSnapshotStore()
	{
		// The MongoDB snapshot fixture is bound to PersistenceAggregate.
		_context = fixture.CreateContext();
		return (IQueryableEventStoreCore<TAggregate>)(object)_context.EventStore;
	}

	protected override Task SnapshotAsync(
		IQueryableEventStoreCore<TAggregate> store,
		TAggregate aggregate,
		CancellationToken cancellationToken
	) =>
		((MongoDBSnapshotEventStore<PersistenceAggregate>)(object)store).SnapshotAsync(
			(PersistenceAggregate)(object)aggregate,
			cancellationToken
		);
}
