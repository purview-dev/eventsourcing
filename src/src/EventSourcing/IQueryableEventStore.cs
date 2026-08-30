using System.Linq.Expressions;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing;

/// <summary>
/// Provides querying and sorting operations to an <see cref="IEventStore"/>.
/// </summary>
public interface IQueryableEventStore : IEventStore
{
	/// <summary>
	/// Streams the query results as an asynchronous enumerable with per-operation paging.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="maxRecordsPerIteration">The maximum number of records fetched per continuation operation.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>An asynchronous enumerable of matching aggregates.</returns>
	IAsyncEnumerable<T> GetQueryEnumerableAsync<T>(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		int maxRecordsPerIteration = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new();

	/// <summary>
	/// Streams all results as an asynchronous enumerable with per-operation paging.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="maxRecordsPerIteration">The maximum number of records fetched per continuation operation.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>An asynchronous enumerable of aggregates.</returns>
	IAsyncEnumerable<T> GetListEnumerableAsync<T>(
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		int maxRecordsPerIteration = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new();

	/// <summary>
	/// Queries a page of aggregates matching the filter.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="request">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	Task<ContinuationResponse<T>> QueryAsync<T>(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		ContinuationRequest request,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new();

	/// <summary>
	/// Lists a page of aggregates.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="request">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	Task<ContinuationResponse<T>> ListAsync<T>(
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		ContinuationRequest request,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new();

	/// <summary>
	/// Counts the aggregates matching the optional filter.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="whereClause">Optional filter; when null, all aggregates are counted.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The number of matching aggregates.</returns>
	Task<long> CountAsync<T>(Expression<Func<T, bool>>? whereClause, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new();

	/// <summary>
	/// Gets the single aggregate matching the filter, throwing when more than one matches.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The single matching aggregate, or null.</returns>
	Task<T?> SingleOrDefaultAsync<T>(
		Expression<Func<T, bool>> whereClause,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new();

	/// <summary>
	/// Gets the first aggregate matching the filter.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The first matching aggregate, or null.</returns>
	Task<T?> FirstOrDefaultAsync<T>(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new();
}
