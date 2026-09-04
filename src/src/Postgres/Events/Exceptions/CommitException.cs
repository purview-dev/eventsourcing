namespace Purview.EventSourcing.Postgres.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when committing events to the PostgreSQL event store fails.
/// </summary>
/// <param name="aggregateId">The id of the aggregate whose events failed to commit.</param>
/// <param name="idempotencyId">The idempotency id of the operation that failed to commit.</param>
/// <param name="versionAttempted">The aggregate version the operation attempted to commit.</param>
/// <param name="version">The aggregate version currently persisted in the store.</param>
/// <param name="exception">The underlying exception that caused the commit to fail.</param>
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
	/// The id of the aggregate whose events failed to commit.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The aggregate version the operation attempted to commit.
	/// </summary>
	public int VersionAttempted { get; } = versionAttempted;

	/// <summary>
	/// The idempotency id of the operation that failed to commit.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));

	/// <summary>
	/// The aggregate version currently persisted in the store.
	/// </summary>
	public int Version { get; } = version;
}
