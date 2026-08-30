namespace Purview.EventSourcing.Postgres.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an aggregate does not have an expected stream version row.
/// </summary>
/// <param name="aggregateId">The id of the aggregate missing its stream version.</param>
/// <param name="idempotencyId">The idempotency id of the operation.</param>
public class AggregateStreamVersionMissingException(string aggregateId, string idempotencyId)
	: Exception(
		$"An attempt to delete an aggregate (Id: {aggregateId}) that doesn't a stream version deleted was made. {nameof(idempotencyId)}: {idempotencyId}."
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The id of the aggregate missing its stream version.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// The idempotency id of the operation.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));
}
