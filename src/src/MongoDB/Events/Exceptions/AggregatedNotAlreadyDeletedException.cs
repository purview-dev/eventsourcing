namespace Purview.EventSourcing.MongoDB.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to restore an aggregate that is not currently deleted.
/// </summary>
/// <param name="aggregateId">The identifier of the aggregate.</param>
/// <param name="idempotencyId">The idempotency identifier of the operation.</param>
public class AggregatedNotAlreadyDeletedException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to restore an aggregate (Id: {aggregateId}) that is not deleted was made.\n\n\tIdempotencyId: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The identifier of the aggregate that is not deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The idempotency identifier of the restore operation.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
