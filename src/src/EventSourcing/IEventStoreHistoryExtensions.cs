using System.Diagnostics.CodeAnalysis;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Internal;

namespace Purview.EventSourcing;

/// <summary>
/// Provides event-history enumeration over an <see cref="IEventStore"/>.
/// </summary>
[System.Diagnostics.DebuggerStepThrough]
[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public static class IEventStoreHistoryExtensions
{
	extension([NotNull] IEventStore eventStore)
	{
		/// <summary>
		/// Enumerates the event history for the aggregate, honoring the paging, version, and time filters in
		/// <paramref name="request"/>.
		/// </summary>
		/// <typeparam name="T">The aggregate type.</typeparam>
		/// <param name="aggregateId">The id of the aggregate whose history should be enumerated.</param>
		/// <param name="request">The paging and filtering options; when null, the defaults are used.</param>
		/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
		/// <returns>A <see cref="ContinuationResponse{AggregateEventHistoryItem}"/> containing the requested history items
		/// and a continuation token when more results are available.</returns>
		/// <exception cref="NotSupportedException">Thrown when the configured store does not support event history enumeration.</exception>
		public Task<ContinuationResponse<AggregateEventHistoryItem>> GetEventHistoryAsync<T>(
			string aggregateId,
			AggregateEventHistoryRequest? request = null,
			CancellationToken cancellationToken = default
		)
			where T : class, IAggregate, new()
		{
			ArgumentNullException.ThrowIfNull(eventStore);

			var typedStore = (eventStore as IEventStoreImplementationAccessor)?.GetEventStore<T>();
			return typedStore == null
				? throw new NotSupportedException(
					$"The configured event store '{eventStore.GetType().FullName}' does not expose implementation accessors required for event history."
				)
				: typedStore.GetEventHistoryAsync(aggregateId, request, cancellationToken);
		}
	}
}
