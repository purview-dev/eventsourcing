namespace Purview.EventSourcing.AzureStorage.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to save an aggregate that does not have an id.
/// </summary>
/// <param name="idempotencyId">The idempotency id associated with the save attempt.</param>
public class MissingAggregateIdException(string idempotencyId)
	: Exception($"An attempt to save an aggregate is a missing Id was made, {nameof(idempotencyId)}: {idempotencyId}.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the idempotency id associated with the save attempt.
	/// </summary>
	public string IdempotencyId { get; set; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
