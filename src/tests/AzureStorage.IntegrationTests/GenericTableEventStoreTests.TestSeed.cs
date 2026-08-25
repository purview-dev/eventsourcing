using System.Security.Cryptography;
using Azure.Data.Tables;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Persistence.Events;
using Purview.EventSourcing.AzureStorage.Entities;
using Purview.EventSourcing.AzureStorage.Exceptions;
using Purview.EventSourcing.AzureStorage.StorageClients.Table;

namespace Purview.EventSourcing.AzureStorage;

/// <summary>
/// Centralizes the aggregate construction, store seeding, and shared assertion
/// logic that is duplicated across <see cref="GenericTableEventStoreTests{TAggregate}" />
/// so the test class and its methods stay below the CA1506 coupling thresholds.
/// </summary>
static class GenericTableEventStoreTestSeed
{
	internal static ComplexTestType CreateComplexTestType()
	{
		return new()
		{
			Int16Property = (short)RandomNumberGenerator.GetInt32(short.MinValue, short.MaxValue),
			Int32Property = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue),
			Int64Property = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue) * 5L,
			StringProperty = $"{Guid.NewGuid()}",
			DateTimeOffsetProperty = DateTimeOffset.UtcNow.AddYears(RandomNumberGenerator.GetInt32(100, 1001)),
			ComplexNestedTestTypeProperty = new() { Nested = $"Nested_{Guid.NewGuid()}" },
		};
	}

	internal static TAggregate BuildAggregateWithIncrementEvents<TAggregate>(string aggregateId, int eventCount)
		where TAggregate : class, IAggregateTest, new()
	{
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId);

		for (var i = 0; i < eventCount; i++)
			aggregate.IncrementInt32Value();

		return aggregate;
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
		TableEventStore<TAggregate> eventStore,
		AzureTableClient tableClient,
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

			batch.Update(eventToUpdate, merge: false);
		}

		await tableClient.SubmitBatchAsync(batch, cancellationToken);
	}

	internal static void AssertCannotApplyEventForOldEvents(
		ITableEventStoreTelemetryMock telemetry,
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
				Is<string>(eventType => eventType!.Contains(nameof(OldEvent), StringComparison.Ordinal)),
				Any<int>()
			)
			.WasCalled(Times.Exactly(numberOfOldEventsToCreate));
	}

	internal static void AssertSkippedUnknownEvent(
		ITableEventStoreTelemetryMock telemetry,
		string aggregateId,
		string unknownEventType,
		int numberOfOldEventsToCreate
	)
	{
		telemetry
			.SkippedUnknownEvent(aggregateId, Any<string>(), Any<string>(), unknownEventType, Any<int>())
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

	internal static async Task AssertRecreatedMatchesSource<TAggregate>(TAggregate? result, TAggregate aggregate)
		where TAggregate : class, IAggregateTest, new()
	{
		await Assert.That(result).IsNotNull();
		await Assert.That(result.Id()).IsEqualTo(aggregate.Id());
		await Assert.That(result.IncrementInt32).IsEqualTo(aggregate.IncrementInt32);
		await Assert.That(result.Details.SavedVersion).IsEqualTo(aggregate.Details.SavedVersion);
		await Assert.That(result.Details.CurrentVersion).IsEqualTo(aggregate.Details.CurrentVersion);
		await Assert.That(result.Details.SnapshotVersion).IsEqualTo(aggregate.Details.SnapshotVersion);
	}

	internal static async Task<int?> ReadAndRemoveStreamVersion(
		AzureTableClient tableClient,
		string aggregateId,
		CancellationToken cancellationToken
	)
	{
		// Get and update stream version to remove the Version property.
		var streamVersion =
			await tableClient.GetAsync<TableEntity>(
				aggregateId,
				TableEventStoreConstants.StreamVersionRowKey,
				cancellationToken: cancellationToken
			) ?? throw new NullReferenceException();
		var streamVersionVersion = streamVersion[nameof(StreamVersionEntity.Version)] as int?;

		streamVersion.Remove(nameof(StreamVersionEntity.Version));

		await tableClient.OperationAsync(
			TableTransactionActionType.UpdateReplace,
			streamVersion,
			cancellationToken: cancellationToken
		);

		return streamVersionVersion;
	}

	internal static async Task<int?> GetStreamVersionNumberAndAssertNotNull(
		AzureTableClient tableClient,
		string aggregateId,
		CancellationToken cancellationToken
	)
	{
		// Get and update stream version to remove the Version property.
		var streamVersion = await tableClient.GetAsync<TableEntity>(
			aggregateId,
			TableEventStoreConstants.StreamVersionRowKey,
			cancellationToken: cancellationToken
		);

		await Assert.That(streamVersion).IsNotNull();

		return streamVersion![nameof(StreamVersionEntity.Version)] as int?;
	}

	internal static async Task AssertGetAsyncThrowsWhenDeleted<TAggregate>(
		TableEventStore<TAggregate> eventStore,
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
		TableEventStore<TAggregate> eventStore,
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
		where TAggregate : class, IAggregateTest, new() => aggregate.SetComplexProperty(CreateComplexTestType());

	internal static async Task AssertComplexPropertyMatches<TAggregate>(
		TAggregate aggregate,
		TAggregate? aggregateGetResult
	)
		where TAggregate : class, IAggregateTest, new()
	{
		await Assert.That(aggregateGetResult).IsNotNull();
		await Assert.That(aggregate.ComplexTestType).IsEquivalentTo(aggregateGetResult.ComplexTestType);
	}
}
