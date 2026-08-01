using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Snapshotting;
using Purview.EventSourcing.ChangeFeed;
using Purview.EventSourcing.Postgres.Snapshot;
using Purview.EventSourcing.Postgres.Snapshots;

namespace Purview.EventSourcing.Fixtures.Postgres;

public class PostgresSnapshotEventStoreFixture : PostgresEventStoreFixture
{
	public PostgresSnapshotEventStore<TAggregate> CreateSnapshotStore<TAggregate>(
		ISnapshotStrategy<TAggregate>? snapshotStrategy = null,
		ISnapshotStrategySelector? snapshotStrategySelector = null,
		IAggregateChangeFeedNotifier<TAggregate>? aggregateChangeNotifier = null,
		bool removeFromCacheOnDelete = false,
		Guid? runId = null,
		Action<PostgresSnapshotEventStoreOptions>? configureOptions = null
	)
		where TAggregate : class, IAggregate, new()
	{
		runId ??= Guid.NewGuid();
		var eventStore = CreateEventStore(
			aggregateChangeNotifier,
			removeFromCacheOnDelete,
			runId,
			configureOptions: null
		);
		PostgresSnapshotEventStoreOptions config = new()
		{
			ConnectionString = ConnectionString,
			TableName = $"EventStoreSnapshots_{runId:N}",
			SchemaName = "public",
			AutoCreateTable = true,
		};
		configureOptions?.Invoke(config);

		PostgresSnapshotEventStore<TAggregate> snapshotStore = new(
			eventStore,
			Microsoft.Extensions.Options.Options.Create(config),
			IPostgresSnapshotEventStoreTelemetry.Mock(),
			snapshotStrategy: snapshotStrategy,
			snapshotStrategySelector: snapshotStrategySelector
		);

		return snapshotStore;
	}
}
