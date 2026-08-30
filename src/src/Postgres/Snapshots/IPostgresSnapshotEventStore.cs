using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Postgres.Snapshots;

/// <summary>
/// The PostgreSQL queryable snapshot event-store contract for <typeparamref name="T"/> aggregates.
/// </summary>
/// <typeparam name="T">An <see cref="IAggregate"/> implementation.</typeparam>
public interface IPostgresSnapshotEventStore<T> : IQueryableEventStoreCore<T>
	where T : class, IAggregate, new()
{
	/// <summary>
	/// This will force snapshot the aggregate regardless of it's save state in the internal event store.
	/// </summary>
	/// <param name="aggregate">The aggregate to upsert.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task.</returns>
	Task SnapshotAsync(T aggregate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Queries a page of snapshots whose serialized payload contains the specified JSON fragment.
	/// </summary>
	/// <param name="jsonFragment">A JSON fragment that the payload must contain.</param>
	/// <param name="request">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	Task<ContinuationResponse<T>> WherePayloadContainsAsync(
		string jsonFragment,
		ContinuationRequest request,
		CancellationToken cancellationToken = default
	);

	/// <summary>
	/// Queries a page of snapshots whose serialized payload contains the specified JSON key.
	/// </summary>
	/// <param name="key">A JSON key that the payload must contain.</param>
	/// <param name="request">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	Task<ContinuationResponse<T>> WherePayloadHasKeyAsync(
		string key,
		ContinuationRequest request,
		CancellationToken cancellationToken = default
	);
}
