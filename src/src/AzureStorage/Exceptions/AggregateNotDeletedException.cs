namespace Purview.EventSourcing.AzureStorage.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an attempt is made to get an aggregate that is not deleted.
/// </summary>
/// <param name="aggregateId">The id of the aggregate.</param>
public class AggregateNotDeletedException(string aggregateId)
	: Exception($"An attempt to get an aggregate that has not been deleted, aggregate Id: {aggregateId}.")
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the id of the aggregate that is not deleted.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
}
