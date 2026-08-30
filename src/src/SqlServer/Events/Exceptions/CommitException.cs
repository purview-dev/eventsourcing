namespace Purview.EventSourcing.SqlServer.Events.Exceptions;

/// <summary>
/// Thrown when a batch of events fails to commit to the SQL Server event store.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
/// <param name="idempotencyId">The idempotency identifier of the save operation.</param>
/// <param name="versionAttempted">The aggregate version that was attempted.</param>
/// <param name="version">The version currently present in the store.</param>
/// <param name="exception">The underlying exception that caused the commit to fail.</param>
#pragma warning disable CA1032 // Implement standard exception constructors
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
	/// The id of the aggregate that failed to commit.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The aggregate version that was attempted.
	/// </summary>
	public int VersionAttempted { get; } = versionAttempted;

	/// <summary>
	/// The idempotency identifier of the save operation.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));

	/// <summary>
	/// The version currently present in the store.
	/// </summary>
	public int Version { get; } = version;
}
