namespace Purview.EventSourcing.SqlServer.Events.Exceptions;

/// <summary>
/// Thrown when an attempt is made to save an aggregate that is currently locked.
/// </summary>
/// <param name="idempotencyId">The idempotency identifier of the save operation.</param>
#pragma warning disable CA1032 // Implement standard exception constructors
public class AggregateLockedException(string idempotencyId)
	: Exception(
		$"An attempt to save an aggregate that is currently locked was made, {nameof(idempotencyId)}: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The idempotency identifier of the save operation.
	/// </summary>
	public string IdempotencyId { get; set; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
