namespace Purview.EventSourcing;

/// <summary>
/// Provides a retry-with-backoff policy for optimistic-concurrency write conflicts.
/// </summary>
/// <remarks>
/// <para>
/// The framework itself does not retry failed writes; a <c>ConcurrencyException</c> is surfaced
/// to the caller. Callers that need a read-modify-write loop (load an aggregate, apply domain
/// changes, save, and re-read + re-apply on a conflict) should wrap their operation with
/// <see cref="ExecuteAsync{TResult}(Func{Task{TResult}}, int, TimeSpan?, Func{Exception, bool}?, CancellationToken)"/>.
/// </para>
/// <para>
/// By default the policy only retries exceptions that implement <see cref="IConcurrencyConflict"/>,
/// which all provider <c>ConcurrencyException</c> types implement. Provide a custom predicate to
/// widen or narrow the set of retryable failures.
/// </para>
/// </remarks>
public static class ConcurrencyRetry
{
	/// <summary>
	/// Executes <paramref name="operation"/> retrying it with exponential backoff when it throws a
	/// concurrency-conflict exception. After <paramref name="maxAttempts"/> attempts the last
	/// exception is rethrown.
	/// </summary>
	/// <typeparam name="TResult">The result type of the operation.</typeparam>
	/// <param name="operation">The operation to execute, typically a load → modify → save loop.</param>
	/// <param name="maxAttempts">The maximum number of attempts (inclusive), at least 1.</param>
	/// <param name="initialBackoff">The base delay before the first retry; doubles each attempt.</param>
	/// <param name="isConcurrencyException">
	/// Determines whether an exception is retryable. Defaults to testing for <see cref="IConcurrencyConflict"/>.
	/// </param>
	/// <param name="cancellationToken">The stopping token, also cancels the backoff delay.</param>
	/// <returns>The result of a successful attempt.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxAttempts"/> is less than 1.</exception>
	public static async Task<TResult> ExecuteAsync<TResult>(
		Func<Task<TResult>> operation,
		int maxAttempts = 3,
		TimeSpan? initialBackoff = null,
		Func<Exception, bool>? isConcurrencyException = null,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(operation);
		if (maxAttempts < 1)
			throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "Must be at least 1.");

		isConcurrencyException ??= static ex => ex is IConcurrencyConflict;
		var backoff = initialBackoff ?? TimeSpan.FromMilliseconds(50);

		var attempt = 0;
		while (true)
		{
			attempt++;
			try
			{
				return await operation().ConfigureAwait(false);
			}
			catch (Exception ex) when (isConcurrencyException(ex) && attempt < maxAttempts)
			{
				var delay = backoff * Math.Pow(2, attempt - 1);
				await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
