namespace Purview.EventSourcing.MongoDB.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to get an aggregate that has not been deleted using a deleted-aggregate operation.
/// </summary>
/// <param name="aggregateId">The identifier of the aggregate.</param>
public class AggregateNotDeletedException(string aggregateId)
	: Exception($"An attempt to get an aggregate that has not been deleted, aggregate Id: {aggregateId}.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The identifier of the aggregate that has not been deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
}
