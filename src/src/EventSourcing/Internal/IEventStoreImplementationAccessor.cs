using System.ComponentModel;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Internal;

/// <summary>
/// Provides access to the concrete <see cref="IEventStoreCore{T}"/> implementation behind a store facade.
/// </summary>
/// <remarks>
/// Implemented by store facades so provider-specific behavior can be reached without down-casting the
/// public store abstraction. Hidden from IntelliSense as this is framework plumbing rather than public API.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IEventStoreImplementationAccessor
{
	/// <summary>
	/// Gets the concrete event store implementation for the specified aggregate type.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <returns>The <see cref="IEventStoreCore{T}"/> implementation.</returns>
	IEventStoreCore<T> GetEventStore<T>()
		where T : class, IAggregate, new();
}
