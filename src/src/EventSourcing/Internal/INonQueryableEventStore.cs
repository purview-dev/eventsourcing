using System.ComponentModel;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Internal;

/// <summary>
/// Marker interface identifying an <see cref="IEventStoreCore{T}"/> implementation that does not support
/// queryable reads.
/// </summary>
/// <typeparam name="T">The aggregate type.</typeparam>
/// <remarks>
/// Implemented by event-store providers that persist events but do not maintain a queryable snapshot model.
/// Hidden from IntelliSense as this is framework plumbing rather than public API.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface INonQueryableEventStore<T> : IEventStoreCore<T>
	where T : class, IAggregate, new()
{
	//
}
