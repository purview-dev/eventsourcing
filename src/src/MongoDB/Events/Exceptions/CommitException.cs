namespace Purview.EventSourcing.MongoDB.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when a batch of events fails to commit to MongoDB storage.
/// </summary>
/// <param name="aggregateId">The identifier of the aggregate being saved.</param>
/// <param name="idempotencyId">The idempotency identifier of the operation.</param>
/// <param name="versionAttempted">The aggregate version that was attempted to be saved.</param>
/// <param name="version">The current aggregate version present in storage.</param>
/// <param name="exception">The underlying storage exception that caused the commit to fail.</param>
public class CommitException(
	string aggregateId,
	string idempotencyId,
	int versionAttempted,
	int version,
	Exception exception
)
	: Exception(
		$"Failed to commit events.\n\tAggregateId: {aggregateId}\n\tIdempotencyId: {idempotencyId}\n\tVersionAttempted: {versionAttempted}\n\tVersionPresent: {version}",
		exception
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The identifier of the aggregate being saved.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The aggregate version that was attempted to be saved.
	/// </summary>
	public int VersionAttempted { get; } = versionAttempted;

	/// <summary>
	/// The idempotency identifier of the save operation.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));

	/// <summary>
	/// The current aggregate version present in storage.
	/// </summary>
	public int Version { get; } = version;
}
