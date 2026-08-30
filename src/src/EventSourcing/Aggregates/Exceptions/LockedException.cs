namespace Purview.EventSourcing.Aggregates.Exceptions;

/// <summary>
/// Indicates the <see cref="IAggregate"/> is in a locked state, and
/// cannot be modified or saved.
/// </summary>
/// <seealso cref="AggregateDetails.Locked"/>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public sealed class LockedException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LockedException"/> class for the specified aggregate.
	/// </summary>
	/// <param name="aggregateId">The Id of the locked aggregate.</param>
	/// <param name="message">An optional custom error message.</param>
	public LockedException(string aggregateId, string? message = null)
		: base(message ?? CreateMessage(aggregateId))
	{
		AggregateId = aggregateId;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LockedException"/> class with a reference to the inner
	/// exception that is the cause of this exception.
	/// </summary>
	/// <param name="aggregateId">The Id of the locked aggregate.</param>
	/// <param name="inner">The exception that is the cause of the current exception.</param>
	/// <param name="message">An optional custom error message.</param>
	public LockedException(string aggregateId, Exception inner, string? message = null)
		: base(message ?? CreateMessage(aggregateId), inner)
	{
		AggregateId = aggregateId;
	}

	/// <summary>
	/// Gets or sets the Id of the locked aggregate.
	/// </summary>
	public string AggregateId { get; set; }

	static string CreateMessage(string aggregateId) =>
		$"The aggregate with Id '{aggregateId}' is locked for modifications.";
}
