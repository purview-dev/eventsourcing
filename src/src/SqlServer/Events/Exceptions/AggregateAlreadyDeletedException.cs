namespace Purview.EventSourcing.SqlServer.Events.Exceptions;

/// <summary>
/// Thrown when an attempt is made to delete an aggregate that is already deleted.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
/// <param name="idempotencyId">The idempotency identifier of the delete operation.</param>
#pragma warning disable CA1032 // Implement standard exception constructors
public class AggregateAlreadyDeletedException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to delete an aggregate (Id: {aggregateId}) that is already deleted was made. {nameof(idempotencyId)}: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The id of the aggregate that was already deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The idempotency identifier of the delete operation.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
