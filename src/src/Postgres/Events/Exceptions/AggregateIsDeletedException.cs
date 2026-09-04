namespace Purview.EventSourcing.Postgres.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an operation is attempted against an aggregate that has been deleted.
/// </summary>
/// <param name="aggregateId">The id of the deleted aggregate.</param>
public class AggregateIsDeletedException(string aggregateId)
	: Exception($"Invalid operation against an aggregate (Id: {aggregateId}) that has been deleted.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The id of the deleted aggregate.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
}
