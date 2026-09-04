using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing;

/// <summary>
/// Convenience extension methods over <see cref="IQueryableEventStore"/> for query, list, count, and
/// first-or-default workflows.
/// </summary>
/// <remarks>
/// These helpers reduce boilerplate by supplying default ordering and paging, and by converting count-based
/// paging into <see cref="ContinuationRequest"/> values. They are hidden from IntelliSense as they are
/// intended for use through the main <see cref="IQueryableEventStore"/> facade.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[System.Diagnostics.DebuggerStepThrough]
public static class IQueryableEventStoreExtensions
{
	#region GetQueryEnumerableAsync

	/// <summary>
	/// Streams the query results as an asynchronous enumerable with default ordering and per-operation paging.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="maxRecordsPerOperation">The maximum number of records fetched per continuation operation.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>An asynchronous enumerable of matching aggregates.</returns>
	public static IAsyncEnumerable<T> GetQueryEnumerableAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.GetQueryEnumerableAsync(whereClause, null, maxRecordsPerOperation, cancellationToken);

	/// <summary>
	/// Streams the query results as an asynchronous enumerable with ascending ordering and per-operation paging.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <typeparam name="TOrderBy">The type of the ordering key.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByAscending">The ascending ordering expression.</param>
	/// <param name="maxRecordsPerOperation">The maximum number of records fetched per continuation operation.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>An asynchronous enumerable of matching aggregates.</returns>
	public static IAsyncEnumerable<T> GetQueryEnumerableAsync<T, TOrderBy>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		Expression<Func<T, TOrderBy>> orderByAscending,
		int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.GetQueryEnumerableAsync(
			whereClause,
			m => m.OrderBy(orderByAscending),
			maxRecordsPerOperation,
			cancellationToken
		);

	#endregion GetQueryEnumerableAsync

	#region GetListEnumerableAsync

	/// <summary>
	/// Streams all results as an asynchronous enumerable with default ordering and per-operation paging.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="maxRecordsPerOperation">The maximum number of records fetched per continuation operation.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>An asynchronous enumerable of aggregates.</returns>
	public static IAsyncEnumerable<T> GetListEnumerableAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.GetListEnumerableAsync<T>(null, maxRecordsPerOperation, cancellationToken);

	/// <summary>
	/// Streams all results as an asynchronous enumerable with ascending ordering and per-operation paging.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <typeparam name="TOrderBy">The type of the ordering key.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="orderByAscending">The ascending ordering expression.</param>
	/// <param name="maxRecordsPerOperation">The maximum number of records fetched per continuation operation.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>An asynchronous enumerable of aggregates.</returns>
	public static IAsyncEnumerable<T> GetListEnumerableAsync<T, TOrderBy>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, TOrderBy>> orderByAscending,
		int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.GetListEnumerableAsync<T>(
			m => m.OrderBy(orderByAscending),
			maxRecordsPerOperation,
			cancellationToken
		);

	#endregion GetListEnumerableAsync

	#region QueryAsync

	/// <summary>
	/// Queries a page of aggregates matching the filter, with the supplied ordering and page size.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="maxRecordCount">The maximum number of records to return.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> QueryAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		int maxRecordCount = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.QueryAsync(
			whereClause,
			orderByClause,
			new ContinuationRequest { MaxRecords = maxRecordCount },
			cancellationToken
		);

	/// <summary>
	/// Queries a page of aggregates matching the filter using the supplied continuation request.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="continuationRequest">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> QueryAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		ContinuationRequest continuationRequest,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.QueryAsync(whereClause, null, continuationRequest, cancellationToken);

	/// <summary>
	/// Queries a page of aggregates matching the filter with the supplied page size.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="maxRecordCount">The maximum number of records to return.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> QueryAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		int maxRecordCount = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.QueryAsync(
			whereClause,
			null,
			new ContinuationRequest { MaxRecords = maxRecordCount },
			cancellationToken
		);

	/// <summary>
	/// Queries a page of aggregates matching the filter, ordered by the ascending ordering expression.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">The ascending ordering expression.</param>
	/// <param name="continuationRequest">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> QueryAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		Expression<Func<T, bool>> orderByClause,
		ContinuationRequest continuationRequest,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.QueryAsync(whereClause, m => m.OrderBy(orderByClause), continuationRequest, cancellationToken);

	/// <summary>
	/// Queries a page of aggregates matching the filter, ordered by the ascending ordering expression with the
	/// supplied page size.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">The ascending ordering expression.</param>
	/// <param name="maxRecordCount">The maximum number of records to return.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> QueryAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		Expression<Func<T, bool>> orderByClause,
		int maxRecordCount = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.QueryAsync(whereClause, m => m.OrderBy(orderByClause), maxRecordCount, cancellationToken);

	#endregion QueryAsync

	#region ListAsync

	/// <summary>
	/// Lists a page of aggregates with the supplied ordering and page size.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="orderByClause">Optional ordering applied to the results.</param>
	/// <param name="maxRecordCount">The maximum number of records to return.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> ListAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		int maxRecordCount = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.ListAsync(orderByClause, new ContinuationRequest { MaxRecords = maxRecordCount }, cancellationToken);

	/// <summary>
	/// Lists a page of aggregates using the supplied continuation request.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="continuationRequest">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> ListAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		ContinuationRequest continuationRequest,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() => eventStore.ListAsync<T>(null, continuationRequest, cancellationToken);

	/// <summary>
	/// Lists a page of aggregates with the supplied page size.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="maxRecordCount">The maximum number of records to return.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> ListAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		int maxRecordCount = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.ListAsync<T>(null, new ContinuationRequest { MaxRecords = maxRecordCount }, cancellationToken);

	/// <summary>
	/// Lists a page of aggregates ordered by the ascending ordering expression using the supplied continuation request.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="orderByClause">The ascending ordering expression.</param>
	/// <param name="continuationRequest">The paging request.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> ListAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> orderByClause,
		ContinuationRequest continuationRequest,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.ListAsync<T>(m => m.OrderBy(orderByClause), continuationRequest, cancellationToken);

	/// <summary>
	/// Lists a page of aggregates ordered by the ascending ordering expression with the supplied page size.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="orderByClause">The ascending ordering expression.</param>
	/// <param name="maxRecordCount">The maximum number of records to return.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ContinuationResponse{T}"/> containing the page and a continuation token.</returns>
	public static Task<ContinuationResponse<T>> ListAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> orderByClause,
		int maxRecordCount = ContinuationRequest.DefaultMaxRecords,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.ListAsync<T>(m => m.OrderBy(orderByClause), maxRecordCount, cancellationToken);

	#endregion ListAsync

	#region CountAsync

	/// <summary>
	/// Counts the aggregates in the snapshot store.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The number of aggregates.</returns>
	public static Task<long> CountAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() => eventStore.CountAsync<T>(null, cancellationToken);

	#endregion CountAsync

	#region FirstOrDefaultAsync

	/// <summary>
	/// Gets the first aggregate matching the filter.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The first matching aggregate, or null.</returns>
	public static Task<T?> FirstOrDefaultAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() => eventStore.FirstOrDefaultAsync(whereClause, null, cancellationToken);

	/// <summary>
	/// Gets the first aggregate matching the filter, ordered by the ascending ordering expression.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="eventStore">The <see cref="IQueryableEventStore"/> used as the root object.</param>
	/// <param name="whereClause">The filter applied to the snapshot query.</param>
	/// <param name="orderByClause">The ascending ordering expression.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>The first matching aggregate, or null.</returns>
	public static Task<T?> FirstOrDefaultAsync<T>(
		[NotNull] this IQueryableEventStore eventStore,
		Expression<Func<T, bool>> whereClause,
		Expression<Func<T, bool>> orderByClause,
		CancellationToken cancellationToken = default
	)
		where T : class, IAggregate, new() =>
		eventStore.FirstOrDefaultAsync(whereClause, m => m.OrderBy(orderByClause), cancellationToken);

	#endregion FirstOrDefaultAsync
}
