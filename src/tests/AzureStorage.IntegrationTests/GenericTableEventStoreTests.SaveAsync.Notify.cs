using Purview.EventSourcing.Aggregates.Events;
using Purview.EventSourcing.ChangeFeed;

namespace Purview.EventSourcing.AzureStorage;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task SaveAsync_GivenAggregateWithChanges_NotifiesChangeFeed(
		int eventsToCreate,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var aggregateChangeNotifier = TestHelpers.CreateAggregateChangeFeedNotified<TAggregate>();

		var beforeWasCalled = false;
		var afterWasCalled = false;
		var aggregateId = $"{Guid.NewGuid()}";
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId);
		for (var i = 0; i < eventsToCreate; i++)
			aggregate.AppendString($"{i + 1} of {eventsToCreate}(s) to created.");

		var eventStore = fixture.CreateEventStore(aggregateChangeNotifier: aggregateChangeNotifier);

		aggregateChangeNotifier
			.BeforeSaveAsync(aggregate, true, Any<CancellationToken>())
			.Callback(
				(a, _, _) =>
				{
					a.AppendString(nameof(IAggregateChangeFeedProcessor.AfterSaveAsync));

					beforeWasCalled = true;
				}
			);

		aggregateChangeNotifier
			.AfterSaveAsync(aggregate, 0, true, Any<IEvent[]>(), Any<CancellationToken>())
			.Callback(() => afterWasCalled = true);

		// Act
		bool result = await eventStore.SaveAsync(aggregate, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(beforeWasCalled).IsTrue();
		await Assert.That(afterWasCalled).IsTrue();

		aggregateChangeNotifier
			.BeforeSaveAsync(aggregate, true, Any<CancellationToken>())
			.WasCalled(Times.Once);

		aggregateChangeNotifier
			.AfterSaveAsync(
				aggregate,
				0,
				true,
				Is<IEvent[]>(events => events!.Length == eventsToCreate),
				Any<CancellationToken>()
			)
			.WasCalled(Times.Once);
	}

	public async Task SaveAsync_GivenAggregateWithNoChanges_DoesNotNotifyChangeFeed(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var aggregateChangeNotifier = TestHelpers.CreateAggregateChangeFeedNotified<TAggregate>();

		var aggregateId = $"{Guid.NewGuid()}";
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId);

		var eventStore = fixture.CreateEventStore(aggregateChangeNotifier: aggregateChangeNotifier);

		// Act
		await eventStore.SaveAsync(aggregate, cancellationToken: cancellationToken);

		// Assert
		aggregateChangeNotifier
			.AfterSaveAsync(aggregate, 0, true, Any<IEvent[]>(), Any<CancellationToken>())
			.WasNeverCalled();
	}
}
