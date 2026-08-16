using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Fixtures.MongoDB;
using Purview.EventSourcing.MongoDb.Events;

namespace Purview.EventSourcing.MongoDB.Events;

[ClassDataSource<MongoDBEventStoreFixture>(Shared = SharedType.PerTestSession)]
public partial class GenericMongoDBEventStoreTests<TAggregate>(MongoDBEventStoreFixture fixture)
	: IMongoDBEventStoreTests
	where TAggregate : class, IAggregateTest, new()
{
	public Task SaveAsync_GivenEventCountIsGreaterThanMaximumNumberOfAllowedInBatchOperation_BatchesEvents(
		int eventsToGenerate,
		CancellationToken _
	)
	{
		throw new NotImplementedException();
	}
}
