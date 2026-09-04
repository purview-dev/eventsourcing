using System.Collections.Concurrent;

namespace Purview.EventSourcing;

/// <summary>
/// A process-wide, per-stream semaphore that serializes read-modify-write work against the same
/// aggregate inside a single process.
/// </summary>
/// <remarks>
/// <para>
/// Acquire a lease with <see cref="AcquireAsync(string, string, CancellationToken)"/> before
/// performing a load → modify → save sequence for an aggregate. Holding the lease while applying
/// domain changes and saving prevents two threads in the same process from computing the same next
/// aggregate version, avoiding spurious <c>ConcurrencyException</c>s. It does not coordinate across
/// processes; cross-process correctness still relies on the store's optimistic concurrency check.
/// </para>
/// <para>
/// Semaphores are keyed by aggregate stream (aggregate type + id) and retained for the lifetime of
/// the process, so the number of held entries is bounded by the number of distinct streams written.
/// </para>
/// </remarks>
public sealed class AggregateWriteLock : IDisposable, IAsyncDisposable
{
	static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

	readonly SemaphoreSlim _semaphore;
	bool _released;

	AggregateWriteLock(SemaphoreSlim semaphore)
	{
		_semaphore = semaphore;
	}

	/// <summary>
	/// Acquires an exclusive lease for the given aggregate stream, waiting if another writer
	/// currently holds it.
	/// </summary>
	/// <param name="aggregateType">The aggregate's persisted <see cref="Aggregates.IAggregate.AggregateType"/>.</param>
	/// <param name="aggregateId">The aggregate's id.</param>
	/// <param name="cancellationToken">Cancels waiting for the lease.</param>
	/// <returns>A lease to dispose once the read-modify-write work is complete.</returns>
	public static async ValueTask<AggregateWriteLock> AcquireAsync(
		string aggregateType,
		string aggregateId,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType, nameof(aggregateType));
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId, nameof(aggregateId));

		var key = $"{aggregateType}:{aggregateId}";
		var semaphore = Locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));

		await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		return new AggregateWriteLock(semaphore);
	}

	/// <summary>
	/// Releases the lease so the next writer can proceed.
	/// </summary>
	public void Dispose()
	{
		if (_released)
			return;

		_released = true;
		_semaphore.Release();
	}

	/// <summary>
	/// Releases the lease so the next writer can proceed.
	/// </summary>
	public ValueTask DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}
}
