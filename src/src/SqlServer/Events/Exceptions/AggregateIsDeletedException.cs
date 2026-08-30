namespace Purview.EventSourcing.SqlServer.Events.Exceptions;

/// <summary>
/// Thrown when an invalid operation is attempted against an aggregate that has been deleted.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
#pragma warning disable CA1032 // Implement standard exception constructors
public class AggregateIsDeletedException(string aggregateId)
	: Exception($"Invalid operation against an aggregate (Id: {aggregateId}) that has been deleted.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// The id of the aggregate that has been deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
}
