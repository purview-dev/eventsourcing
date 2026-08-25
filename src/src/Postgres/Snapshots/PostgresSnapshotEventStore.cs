using Microsoft.Extensions.Options;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Snapshotting;
using Purview.EventSourcing.Internal;
using Purview.EventSourcing.Postgres.Client;
using Purview.EventSourcing.Postgres.Snapshots;

namespace Purview.EventSourcing.Postgres.Snapshot;

public sealed partial class PostgresSnapshotEventStore<T> : IPostgresSnapshotEventStore<T>, ITransactionalEventStore<T>
	where T : class, IAggregate, new()
{
	readonly IEventStoreCore<T> _eventStore;
	readonly IOptions<PostgresSnapshotEventStoreOptions> _sqlServerEventStoreOptions;
	readonly IPostgresSnapshotEventStoreTelemetry _telemetry;
	readonly ISnapshotStrategy<T> _snapshotStrategy;
	readonly ISnapshotStrategySelector? _snapshotStrategySelector;

	readonly PostgresClient _sqlServerClient;

	readonly Type _aggregateType = typeof(T);
	readonly string _aggregateName;

	static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, string> AggregateTypeNames = new();

	public PostgresSnapshotEventStore(
		// Explicitly request a non-queryable event store.
		INonQueryableEventStore<T> eventStore,
		IOptions<PostgresSnapshotEventStoreOptions> sqlServerEventStoreOptions,
		IPostgresSnapshotEventStoreTelemetry telemetry,
		ISnapshotStrategy<T>? snapshotStrategy = null,
		ISnapshotStrategySelector? snapshotStrategySelector = null
	)
	{
		_eventStore = eventStore;
		_sqlServerEventStoreOptions = sqlServerEventStoreOptions;
		_telemetry = telemetry;
		_snapshotStrategy = snapshotStrategy ?? new AlwaysSnapshotStrategy<T>();
		_snapshotStrategySelector = snapshotStrategySelector;

		_aggregateName = TypeNameHelper.GetName(_aggregateType, "Aggregate");

		var clientOptions = ResolveClientOptions(_sqlServerEventStoreOptions.Value, _aggregateName);
		_sqlServerClient = new(clientOptions);
	}

	/// <summary>
	/// Merges global options with any per-aggregate-type table override.
	/// </summary>
	static PostgresClientOptions ResolveClientOptions(PostgresSnapshotEventStoreOptions options, string aggregateName)
	{
		var schema = options.SchemaName;
		var table = options.TableName;

		if (options.AggregateTableOverrides.TryGetValue(aggregateName, out var ovr) && ovr is not null)
		{
			schema = ovr.SchemaName ?? schema;
			table = ovr.TableName ?? table;
		}

		return new(options.ConnectionString, options.UseDataCompression)
		{
			TableName = table,
			SchemaName = schema,
			AutoCreateTable = options.AutoCreateTable,
			JsonIndexOptions = options.JsonIndexOptions,
		};
	}

	/// <summary>
	/// This will upsert the aggregate in the snapshot store.
	/// <strong>Note</strong> if the aggregate has unsaved events, it will first save those
	/// events to the event store before upserting the snapshot to ensure consistency.
	/// </summary>
	/// <param name="aggregate"></param>
	/// <param name="cancellationToken"></param>
	public async Task SnapshotAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(aggregate, nameof(aggregate));

		if (aggregate.HasUnsavedEvents())
			await _eventStore.SaveAsync(aggregate, cancellationToken);

		_telemetry.SnapshotSaveStart(aggregate.Details.Id, _aggregateName);
		using var activity = _telemetry.SnapshotSave(aggregate.Details.Id, _aggregateName);

		try
		{
			var result = await _sqlServerClient.UpsertAsync(
				aggregate,
				aggregate.Details.Id,
				GetAggregateTypeName(),
				cancellationToken
			);
			if (result)
			{
				_telemetry.SnapshotSaveComplete(aggregate.Details.Id, _aggregateName);
			}
		}
		catch (Exception ex)
		{
			_telemetry.SnapshotSaveFailed(aggregate.Details.Id, _aggregateName, ex);
			throw;
		}
	}

	string GetAggregateTypeName() => AggregateTypeNames.GetOrAdd(_aggregateType, _ => new T().AggregateType);
}
