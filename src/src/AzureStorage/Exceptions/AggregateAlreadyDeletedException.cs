namespace Purview.EventSourcing.AzureStorage.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to delete an aggregate that has already been deleted.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
/// <param name="idempotencyId">The idempotency id associated with the delete attempt.</param>
public class AggregateAlreadyDeletedException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to delete an aggregate (Id: {aggregateId}) that is already deleted was made. {nameof(idempotencyId)}: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the id of the aggregate that was already deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// Gets the idempotency id associated with the delete attempt.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
