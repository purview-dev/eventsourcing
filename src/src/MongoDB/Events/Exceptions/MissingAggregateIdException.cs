namespace Purview.EventSourcing.MongoDB.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to save an aggregate that does not have an identifier.
/// </summary>
/// <param name="idempotencyId">The idempotency identifier of the operation.</param>
public class MissingAggregateIdException(string idempotencyId)
	: Exception($"An attempt to save an aggregate is a missing Id was made, {nameof(idempotencyId)}: {idempotencyId}.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The idempotency identifier of the save operation.
	/// </summary>
	public string IdempotencyId { get; set; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
