using System.ComponentModel;
using System.Linq.Expressions;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing;

/// <summary>
/// Provider-facing typed queryable event-store contract used by concrete implementations and internal infrastructure.
/// </summary>
/// <typeparam name="T">An <see cref="IAggregate"/> implementation.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IQueryableEventStoreCore<T> : IEventStoreCore<T>
	where T : class, IAggregate, new()
{
	/// <summary>
	/// Streams the query results as an asynchronous enumerable with per-operation paging.
	/// </summary>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="maxRecordsPerIteration">The maximum number of records fetched per continuation operation.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>An asynchronous enumerable of matching aggregates.</returns>
	IAsyncEnumerable<T> GetQueryEnumerableAsync(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		int maxRecordsPerIteration = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	);

	/// <summary>
	/// Streams all results as an asynchronous enumerable with per-operation paging.
	/// </summary>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="maxRecordsPerIteration">The maximum number of records fetched per continuation operation.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>An asynchronous enumerable of aggregates.</returns>
	IAsyncEnumerable<T> GetListEnumerableAsync(
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		int maxRecordsPerIteration = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	);

	/// <summary>
	/// Queries a page of aggregates matching the filter.
	/// </summary>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="request">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	Task<ContinuationResponse<T>> QueryAsync(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		ContinuationRequest request,
		CancellationToken cancellationToken = default
	);

	/// <summary>
	/// Lists a page of aggregates.
	/// </summary>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="request">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	Task<ContinuationResponse<T>> ListAsync(
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		ContinuationRequest request,
		CancellationToken cancellationToken = default
	);

	/// <summary>
	/// Counts the aggregates matching the optional filter.
	/// </summary>
	/// <param name="whereClause">Optional filter; when null, all aggregates are counted.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The number of matching aggregates.</returns>
	Task<long> CountAsync(Expression<Func<T, bool>>? whereClause, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the single aggregate matching the filter, throwing when more than one matches.
	/// </summary>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The single matching aggregate, or null.</returns>
	Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> whereClause, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the first aggregate matching the filter.
	/// </summary>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The first matching aggregate, or null.</returns>
	Task<T?> FirstOrDefaultAsync(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		CancellationToken cancellationToken = default
	);
}
