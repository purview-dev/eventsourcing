namespace Purview.EventSourcing.Postgres.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an optimistic concurrency conflict is detected while saving an aggregate.
/// </summary>
/// <param name="aggregateId">The id of the aggregate that could not be saved.</param>
/// <param name="idempotencyId">The idempotency id of the operation that could not be saved.</param>
/// <param name="versionAttempted">The aggregate version the operation attempted to save.</param>
/// <param name="version">The aggregate version currently persisted in the store.</param>
public class ConcurrencyException(string aggregateId, string idempotencyId, int versionAttempted, int version)
	: Exception(
		$"Optimistic concurrency error:\n\tAggregateId: {aggregateId}\n\tIdempotencyId: {idempotencyId}\n\tVersionAttempted:{versionAttempted}\n\tVersionPresent:{version}"
	),
		IConcurrencyConflict
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The id of the aggregate that could not be saved.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The aggregate version the operation attempted to save.
	/// </summary>
	public int VersionAttempted { get; } = versionAttempted;

	/// <summary>
	/// The idempotency id of the operation that could not be saved.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));

	/// <summary>
	/// The aggregate version currently persisted in the store.
	/// </summary>
	public int Version { get; } = version;
}
