namespace Purview.EventSourcing.MongoDB.Events.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an invalid operation is performed against an aggregate that has been deleted.
/// </summary>
/// <param name="aggregateId">The identifier of the aggregate.</param>
public class AggregateIsDeletedException(string aggregateId)
	: Exception($"Invalid operation against an aggregate (Id: {aggregateId}) that has been deleted.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The identifier of the aggregate that has been deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
}
