namespace Purview.EventSourcing.AzureStorage.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to save events to an aggregate that has been deleted.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
/// <param name="idempotencyId">The idempotency id associated with the save attempt.</param>
public class AggregateDeletedException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to save an aggregate that has been deleted, aggregate Id: {aggregateId}, {nameof(IdempotencyId)}: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the id of the aggregate that has been deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// Gets the idempotency id associated with the save attempt.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
