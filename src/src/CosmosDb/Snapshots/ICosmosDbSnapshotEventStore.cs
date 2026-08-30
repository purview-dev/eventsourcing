using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.CosmosDb.Snapshots;

/// <summary>
/// Queryable snapshot event-store contract for the Azure Cosmos DB provider.
/// </summary>
/// <typeparam name="T">An <see cref="IAggregate"/> implementation.</typeparam>
/// <seealso cref="IQueryableEventStoreCore{T}"/>
public interface ICosmosDbSnapshotEventStore<T> : IQueryableEventStoreCore<T>
	where T : class, IAggregate, new()
{
	/// <summary>
	/// This will force snapshot the aggregate regardless of it's save state in the internal event store.
	/// </summary>
	/// <param name="aggregate">The aggregate to upsert.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task.</returns>
	Task SnapshotAsync(T aggregate, CancellationToken cancellationToken = default);
}
