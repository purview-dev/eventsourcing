namespace Purview.EventSourcing.Postgres.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to restore an aggregate that is not currently deleted.
/// </summary>
/// <param name="aggregateId">The id of the aggregate that is not deleted.</param>
/// <param name="idempotencyId">The idempotency id of the operation that attempted the restore.</param>
public class AggregatedNotAlreadyDeletedException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to restore an aggregate (Id: {aggregateId}) that is not deleted was made.\n\n\tIdempotencyId: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The id of the aggregate that is not deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The idempotency id of the operation that attempted the restore.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
