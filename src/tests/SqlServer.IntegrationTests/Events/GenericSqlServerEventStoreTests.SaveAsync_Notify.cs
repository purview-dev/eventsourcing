using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.SqlServer.Events;

partial class GenericSqlServerEventStoreTests<TAggregate>
{
	public async Task SaveAsync_GivenAggregateWithChanges_NotifiesChangeFeed(
		int eventsToCreate,
		CancellationToken cancellationToken
	)
	{
		var aggregateChangeNotifier = TestHelpers.CreateAggregateChangeFeedNotified<TAggregate>();
		var beforeWasCalled = false;
		var afterWasCalled = false;
		var aggregateId = $"{Guid.NewGuid()}";
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId);
		for (var i = 0; i < eventsToCreate; i++)
			aggregate.IncrementInt32Value();

		var eventStore = fixture.CreateEventStore(aggregateChangeNotifier: aggregateChangeNotifier);

		aggregateChangeNotifier
			.BeforeSaveAsync(aggregate, true, Any<CancellationToken>())
			.Callback(() => beforeWasCalled = true);
		aggregateChangeNotifier
			.AfterSaveAsync(aggregate, Any<int>(), true, Any<IEvent[]>(), Any<CancellationToken>())
			.Callback(() => afterWasCalled = true);

		var result = await eventStore.SaveAsync(aggregate, cancellationToken: cancellationToken);

		await Assert.That(result.Saved).IsTrue();
		await Assert.That(beforeWasCalled).IsTrue();
		await Assert.That(afterWasCalled).IsTrue();

		aggregateChangeNotifier
			.BeforeSaveAsync(aggregate, true, Any<CancellationToken>())
			.WasCalled(Times.Once);
		aggregateChangeNotifier
			.AfterSaveAsync(aggregate, Any<int>(), true, Any<IEvent[]>(), Any<CancellationToken>())
			.WasCalled(Times.Once);
	}

	public async Task SaveAsync_GivenAggregateWithNoChanges_DoesNotNotifyChangeFeed(
		CancellationToken cancellationToken
	)
	{
		var aggregateChangeNotifier = TestHelpers.CreateAggregateChangeFeedNotified<TAggregate>();
		var aggregateId = $"{Guid.NewGuid()}";
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId);

		var eventStore = fixture.CreateEventStore(aggregateChangeNotifier: aggregateChangeNotifier);

		var result = await eventStore.SaveAsync(aggregate, cancellationToken: cancellationToken);

		await Assert.That(result.Saved).IsFalse();

		aggregateChangeNotifier
			.BeforeSaveAsync(Any<TAggregate>(), Any<bool>(), Any<CancellationToken>())
			.WasNeverCalled();
		aggregateChangeNotifier
			.AfterSaveAsync(
				Any<TAggregate>(),
				Any<int>(),
				Any<bool>(),
				Any<IEvent[]>(),
				Any<CancellationToken>()
			)
			.WasNeverCalled();
	}
}
