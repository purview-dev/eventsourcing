using System.Security.Cryptography;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Persistence.Events;
using Purview.EventSourcing.MongoDB.Events.Entities;
using Purview.EventSourcing.MongoDB.Events.Exceptions;
using Purview.EventSourcing.MongoDB.StorageClient;

namespace Purview.EventSourcing.MongoDB.Events;

/// <summary>
/// Centralizes the aggregate construction, store seeding, and shared assertion
/// logic that is duplicated across <see cref="GenericMongoDBEventStoreTests{TAggregate}" />
/// so the test class and its methods stay below the CA1506 coupling thresholds.
/// </summary>
static class GenericMongoDBEventStoreTestSeed
{
	internal static ComplexTestType CreateComplexTestType()
	{
		return new()
		{
			Int16Property = (short)RandomNumberGenerator.GetInt32(short.MinValue, short.MaxValue),
			Int32Property = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue),
			Int64Property = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue) * 5L,
			StringProperty = $"{Guid.NewGuid()}",
			DateTimeOffsetProperty = DateTimeOffset.UtcNow.AddYears(
				RandomNumberGenerator.GetInt32(100, 1001)
			),
			ComplexNestedTestTypeProperty = new() { Nested = $"Nested_{Guid.NewGuid()}" },
		};
	}

	internal static TAggregate BuildAggregateWithOldEvents<TAggregate>(
		string aggregateId,
		int eventsToCreate,
		int numberOfOldEventsToCreate
	)
		where TAggregate : class, IAggregateTest, new()
	{
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId);
		// Register the event type here...!
		aggregate.RegisterOldEventType();

		for (var i = 0; i < eventsToCreate; i++)
			aggregate.IncrementInt32Value();

		for (var i = 0; i < numberOfOldEventsToCreate; i++)
			aggregate.SetOldEventValue(Guid.NewGuid());

		return aggregate;
	}

	internal static async Task MarkEventsAsUnknown<TAggregate>(
		MongoDBEventStore<TAggregate> eventStore,
		MongoDBClient eventClient,
		string aggregateId,
		int fromVersion,
		int toVersion,
		string unknownEventType,
		CancellationToken cancellationToken
	)
		where TAggregate : class, IAggregateTest, new()
	{
		// Update existing events to make them unknown types effectively.
		var eventsToUpdate = eventStore.GetEventRangeEntitiesAsync(
			aggregateId,
			fromVersion,
			toVersion,
			cancellationToken
		);

		BatchOperation batchOperation = new();
		var batch = batchOperation;
		await foreach (var eventToUpdate in eventsToUpdate)
		{
			eventToUpdate.EventType = unknownEventType;

			batch.Update(eventToUpdate);
		}

		await eventClient.SubmitBatchAsync(batch, cancellationToken);
	}

	internal static void AssertCannotApplyEventForOldEvents(
		IMongoDBEventStoreTelemetryMock telemetry,
		string aggregateId,
		int numberOfOldEventsToCreate
	)
	{
		telemetry
			.CannotApplyEvent(
				aggregateId,
				Any<string>(),
				Any<string>(),
				Any<string>(),
				Is<string>(eventType =>
					eventType!.Contains(nameof(OldEvent), StringComparison.Ordinal)
				),
				Any<int>()
			)
			.WasCalled(Times.Exactly(numberOfOldEventsToCreate));
	}

	internal static void AssertSkippedUnknownEvent(
		IMongoDBEventStoreTelemetryMock telemetry,
		string aggregateId,
		string unknownEventType,
		int numberOfOldEventsToCreate
	)
	{
		telemetry
			.SkippedUnknownEvent(
				aggregateId,
				Any<string>(),
				Any<string>(),
				unknownEventType,
				Any<int>()
			)
			.WasCalled(Times.Exactly(numberOfOldEventsToCreate));
	}

	internal static async Task AssertRecreatedWithTotals<TAggregate>(
		TAggregate? result,
		TAggregate aggregate,
		int totalEvents
	)
		where TAggregate : class, IAggregateTest, new()
	{
		await Assert.That(result).IsNotNull();
		await Assert.That(result.IsNew()).IsFalse();
		await Assert.That(result.Id()).IsEqualTo(aggregate.Id());
		await Assert.That(result.IncrementInt32).IsEqualTo(aggregate.IncrementInt32);
		await Assert.That(result.Details.SavedVersion).IsEqualTo(totalEvents);
		await Assert.That(result.Details.CurrentVersion).IsEqualTo(totalEvents);
	}

	internal static async Task AssertGetAsyncThrowsWhenDeleted<TAggregate>(
		MongoDBEventStore<TAggregate> eventStore,
		string aggregateId,
		CancellationToken cancellationToken
	)
		where TAggregate : class, IAggregateTest, new()
	{
		// Act
		Task<TAggregate?> Func() =>
			eventStore.GetAsync(
				aggregateId,
				new EventStoreOperationContext { DeleteMode = DeleteHandlingMode.ThrowsException },
				cancellationToken: cancellationToken
			);

		// Assert
		await Assert.That(Func).Throws<AggregateIsDeletedException>();
	}

	internal static async Task AssertSaveThrowsArgumentOutOfRangeException<TAggregate>(
		MongoDBEventStore<TAggregate> eventStore,
		TAggregate aggregate,
		CancellationToken cancellationToken
	)
		where TAggregate : class, IAggregateTest, new()
	{
		// Act
		async Task<SaveResult<TAggregate>?> Func() =>
			await eventStore.SaveAsync(aggregate, cancellationToken: cancellationToken);

		// Get and update stream version to remove the Version property.
		await Assert.That(Func).Throws<ArgumentOutOfRangeException>();
	}

	internal static void SetRandomComplexProperty<TAggregate>(TAggregate aggregate)
		where TAggregate : class, IAggregateTest, new() =>
		aggregate.SetComplexProperty(CreateComplexTestType());

	internal static async Task AssertComplexPropertyMatches<TAggregate>(
		TAggregate aggregate,
		TAggregate? aggregateGetResult
	)
		where TAggregate : class, IAggregateTest, new()
	{
		await Assert.That(aggregateGetResult).IsNotNull();
		await Assert
			.That(aggregate.ComplexTestType)
			.IsEquivalentTo(aggregateGetResult.ComplexTestType);
	}

	internal static async Task DeleteSnapshotAndAssertRemoved(
		MongoDBClient snapshotClient,
		string aggregateId,
		CancellationToken cancellationToken
	)
	{
		var snapshotEntity = await snapshotClient.GetAsync<SnapshotEntity>(
			aggregateId,
			EntityTypes.SnapshotType,
			cancellationToken: cancellationToken
		);

		await Assert.That(snapshotEntity).IsNotNull();

		await snapshotClient.DeleteAsync<SnapshotEntity>(
			m => m.Id == aggregateId,
			cancellationToken: cancellationToken
		);

		snapshotEntity = await snapshotClient.GetAsync<SnapshotEntity>(
			aggregateId,
			EntityTypes.SnapshotType,
			cancellationToken: cancellationToken
		);

		await Assert.That(snapshotEntity).IsNull();
	}
}
