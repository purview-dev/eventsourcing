using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Snapshotting;
using Purview.EventSourcing.ChangeFeed;
using Purview.EventSourcing.SqlServer.Snapshot;
using Purview.EventSourcing.SqlServer.Snapshots;

namespace Purview.EventSourcing.Fixtures.SqlServer;

public class SqlServerSnapshotEventStoreFixture : SqlServerEventStoreFixture
{
	public SqlServerSnapshotEventStore<TAggregate> CreateSnapshotStore<TAggregate>(
		ISnapshotStrategy<TAggregate>? snapshotStrategy = null,
		ISnapshotStrategySelector? snapshotStrategySelector = null,
		IAggregateChangeFeedNotifier<TAggregate>? aggregateChangeNotifier = null,
		bool removeFromCacheOnDelete = false,
		Guid? runId = null,
		Action<SqlServerSnapshotEventStoreOptions>? configureOptions = null
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
		SqlServerSnapshotEventStoreOptions config = new()
		{
			ConnectionString = ConnectionString,
			TableName = $"EventStoreSnapshots_{runId:N}",
			SchemaName = "dbo",
			AutoCreateTable = true,
		};
		configureOptions?.Invoke(config);

		SqlServerSnapshotEventStore<TAggregate> snapshotStore = new(
			eventStore,
			Microsoft.Extensions.Options.Options.Create(config),
			ISqlServerSnapshotEventStoreTelemetry.Mock(),
			snapshotStrategy: snapshotStrategy,
			snapshotStrategySelector: snapshotStrategySelector
		);

		return snapshotStore;
	}
}
