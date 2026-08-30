using System.ComponentModel;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Internal;

/// <summary>
/// Provides access to the concrete <see cref="IQueryableEventStoreCore{T}"/> implementation behind a
/// queryable store facade.
/// </summary>
/// <remarks>
/// Implemented by queryable store facades so provider-specific query behavior can be reached without
/// down-casting the public store abstraction. Hidden from IntelliSense as this is framework plumbing
/// rather than public API.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IQueryableEventStoreImplementationAccessor : IEventStoreImplementationAccessor
{
	/// <summary>
	/// Gets the concrete queryable event store implementation for the specified aggregate type.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <returns>The <see cref="IQueryableEventStoreCore{T}"/> implementation.</returns>
	IQueryableEventStoreCore<T> GetQueryableEventStore<T>()
		where T : class, IAggregate, new();
}
