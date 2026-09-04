namespace Purview.EventSourcing;

/// <summary>
/// Marks an exception as an optimistic-concurrency conflict so framework helpers can distinguish
/// retryable write conflicts from other storage failures across every provider.
/// </summary>
/// <remarks>
/// Each storage provider surfaces write conflicts through its own
/// <c>ConcurrencyException</c> type. Implementing this interface lets a single retry policy
/// (see <see cref="ConcurrencyRetry.ExecuteAsync{TResult}"/>) recognize all of them without
/// referencing provider assemblies.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface IConcurrencyConflict;
