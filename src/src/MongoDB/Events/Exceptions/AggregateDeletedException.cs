namespace Purview.EventSourcing.MongoDB.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to save an aggregate that has been deleted.
/// </summary>
/// <param name="aggregateId">The identifier of the aggregate.</param>
/// <param name="idempotencyId">The idempotency identifier of the operation.</param>
public class AggregateDeletedException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to save an aggregate that has been deleted, aggregate Id: {aggregateId}, {nameof(IdempotencyId)}: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The identifier of the aggregate that has been deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The idempotency identifier of the save operation.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
