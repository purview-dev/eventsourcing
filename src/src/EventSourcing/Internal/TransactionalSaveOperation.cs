using System.ComponentModel;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Internal;

/// <summary>
/// Describes the result of a transactional save and the follow-up work to run after commit or rollback.
/// </summary>
/// <typeparam name="T">The aggregate type.</typeparam>
/// <remarks>
/// Hidden from IntelliSense as this is framework plumbing rather than public API. The after-commit and
/// after-rollback callbacks are invoked by the transaction coordinator once the surrounding transaction
/// completes.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TransactionalSaveOperation<T>(
	SaveResult<T> result,
	Func<CancellationToken, Task>? afterCommit = null,
	Func<CancellationToken, Task>? afterRollback = null
)
	where T : class, IAggregate, new()
{
	static readonly Func<CancellationToken, Task> NoOp = _ => Task.CompletedTask;

	/// <summary>
	/// Gets the result of the save operation.
	/// </summary>
	public SaveResult<T> Result { get; } = result;

	readonly Func<CancellationToken, Task> _afterCommit = afterCommit ?? NoOp;
	readonly Func<CancellationToken, Task> _afterRollback = afterRollback ?? NoOp;

	/// <summary>
	/// Runs the work associated with committing the transaction.
	/// </summary>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public Task AfterCommitAsync(CancellationToken cancellationToken = default) => _afterCommit(cancellationToken);

	/// <summary>
	/// Runs the work associated with rolling back the transaction.
	/// </summary>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public Task AfterRollbackAsync(CancellationToken cancellationToken = default) => _afterRollback(cancellationToken);
}
