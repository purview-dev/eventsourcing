namespace Purview.EventSourcing.AzureStorage.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to restore an aggregate that is not deleted.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
/// <param name="idempotencyId">The idempotency id associated with the restore attempt.</param>
public class AggregatedNotAlreadyDeletedException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to restore an aggregate (Id: {aggregateId}) that is not deleted was made.\n\n\tIdempotencyId: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the id of the aggregate that is not deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// Gets the idempotency id associated with the restore attempt.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
