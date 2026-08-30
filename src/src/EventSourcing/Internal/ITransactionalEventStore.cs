using System.ComponentModel;
using System.Data.Common;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Internal;

/// <summary>
/// An <see cref="IEventStoreCore{T}"/> implementation that can enlist its save operations in an external
/// database transaction.
/// </summary>
/// <typeparam name="T">The aggregate type.</typeparam>
/// <remarks>
/// Implemented by providers backed by a relational database so that event persistence can share a single
/// transaction boundary with other work. Hidden from IntelliSense as this is framework plumbing rather
/// than public API.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITransactionalEventStore<T> : IEventStoreCore<T>
	where T : class, IAggregate, new()
{
	/// <summary>
	/// Gets the key that identifies the transaction boundary this store participates in.
	/// </summary>
	string TransactionBoundaryKey { get; }

	/// <summary>
	/// Creates a <see cref="DbConnection"/> that can be enlisted into the store's transaction.
	/// </summary>
	/// <returns>A connection suitable for transaction enlistment.</returns>
	DbConnection CreateTransactionConnection();

	/// <summary>
	/// Ensures the supplied connection has the schema required for transactional saves.
	/// </summary>
	/// <param name="connection">The connection to configure.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task EnsureTransactionConfiguredAsync(DbConnection connection, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves the aggregate within the supplied connection and transaction.
	/// </summary>
	/// <param name="aggregate">The aggregate to save.</param>
	/// <param name="operationContext">The operational context controlling store behavior.</param>
	/// <param name="connection">The connection to persist within.</param>
	/// <param name="transaction">The transaction to enlist into.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A <see cref="TransactionalSaveOperation{T}"/> describing the save and any after-commit/after-rollback work.</returns>
	Task<TransactionalSaveOperation<T>> SaveInTransactionAsync(
		T aggregate,
		EventStoreOperationContext? operationContext,
		DbConnection connection,
		DbTransaction transaction,
		CancellationToken cancellationToken = default
	);
}
