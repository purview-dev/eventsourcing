using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.MongoDB.Snapshots;

/// <summary>
/// The MongoDB-backed, queryable snapshot event-store contract for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">An <see cref="IAggregate"/> implementation.</typeparam>
/// <remarks>
/// Extends the <see cref="IQueryableEventStoreCore{T}"/> contract with an explicit
/// <see cref="SnapshotAsync(T, CancellationToken)"/> operation that persists the aggregate's current state
/// as a MongoDB document, in addition to the underlying event stream.
/// </remarks>
/// <seealso cref="MongoDBSnapshotEventStore{T}"/>
public interface IMongoDBSnapshotEventStore<T> : IQueryableEventStoreCore<T>
	where T : class, IAggregate, new()
{
	/// <summary>
	/// Persists a snapshot of the aggregate's current state.
	/// </summary>
	/// <param name="aggregate">The aggregate to snapshot.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	Task SnapshotAsync(T aggregate, CancellationToken cancellationToken = default);
}
