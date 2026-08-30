namespace Purview.EventSourcing.AzureStorage.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an operation requires a stream version that does not exist for the aggregate.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
/// <param name="idempotencyId">The idempotency id associated with the operation.</param>
public class AggregateStreamVersionMissingException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to delete an aggregate (Id: {aggregateId}) that doesn't a stream version deleted was made. {nameof(idempotencyId)}: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the id of the aggregate.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// Gets the idempotency id associated with the operation.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
