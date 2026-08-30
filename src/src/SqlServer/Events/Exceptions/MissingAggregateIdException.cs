namespace Purview.EventSourcing.SqlServer.Events.Exceptions;

/// <summary>
/// Thrown when an attempt is made to save an aggregate that is missing an id.
/// </summary>
/// <param name="idempotencyId">The idempotency identifier of the save operation.</param>
#pragma warning disable CA1032 // Implement standard exception constructors
public class MissingAggregateIdException(string idempotencyId)
	: Exception($"An attempt to save an aggregate is a missing Id was made, {nameof(idempotencyId)}: {idempotencyId}.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The idempotency identifier of the save operation.
	/// </summary>
	public string IdempotencyId { get; set; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
