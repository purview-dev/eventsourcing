namespace Purview.EventSourcing.Postgres.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to save an aggregate that has no id.
/// </summary>
/// <param name="idempotencyId">The idempotency id of the operation that attempted the save.</param>
public class MissingAggregateIdException(string idempotencyId)
	: Exception($"An attempt to save an aggregate is a missing Id was made, {nameof(idempotencyId)}: {idempotencyId}.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The idempotency id of the operation that attempted the save.
	/// </summary>
	public string IdempotencyId { get; set; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
