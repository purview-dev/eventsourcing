using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.InMemory.Snapshots;

/// <summary>
/// An in-memory <see cref="IQueryableEventStoreCore{T}"/> implementation that exposes queryable snapshot reads.
/// </summary>
/// <typeparam name="T">An <see cref="IAggregate"/> implementation.</typeparam>
/// <remarks>
/// Implemented by <see cref="InMemorySnapshotStore{T}"/>. Querying is performed over the in-memory
/// aggregates, so this store is intended for testing and single-process scenarios.
/// </remarks>
/// <seealso cref="IInMemoryEventStore{T}"/>
public interface IInMemorySnapshotStore<T> : IQueryableEventStoreCore<T>
	where T : class, IAggregate, new()
{
	//
}
