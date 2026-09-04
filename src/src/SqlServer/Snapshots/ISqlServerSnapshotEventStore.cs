using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.SqlServer.Snapshots;

/// <summary>
/// Provider-facing contract for the SQL Server snapshot (queryable) event store.
/// </summary>
/// <typeparam name="T">An <see cref="IAggregate"/> implementation.</typeparam>
/// <remarks>
/// Extends <see cref="IQueryableEventStoreCore{T}"/> with the ability to force a snapshot upsert of an aggregate.
/// </remarks>
/// <seealso cref="IQueryableEventStoreCore{T}"/>
public interface ISqlServerSnapshotEventStore<T> : IQueryableEventStoreCore<T>
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
