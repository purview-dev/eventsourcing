namespace Purview.EventSourcing.AzureStorage.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an optimistic concurrency check fails while saving an aggregate.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
/// <param name="idempotencyId">The idempotency id associated with the save attempt.</param>
/// <param name="versionAttempted">The aggregate version that was attempted to be saved.</param>
/// <param name="version">The aggregate version present in the store.</param>
public class ConcurrencyException(string aggregateId, string idempotencyId, int versionAttempted, int version)
	: Exception(
		$"Optimistic concurrency error:\n\tAggregateId: {aggregateId}\n\tIdempotencyId: {idempotencyId}\n\tVersionAttempted:{versionAttempted}\n\tVersionPresent:{version}"
	),
		IConcurrencyConflict
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the id of the aggregate.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// Gets the aggregate version that was attempted to be saved.
	/// </summary>
	public int VersionAttempted { get; } = versionAttempted;

	/// <summary>
	/// Gets the idempotency id associated with the save attempt.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));

	/// <summary>
	/// Gets the aggregate version present in the store.
	/// </summary>
	public int Version { get; } = version;
}
