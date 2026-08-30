namespace Purview.EventSourcing.Aggregates.Exceptions;

/// <summary>
/// Indicates the <see cref="AggregateDetails.Id"/> was already set.
/// </summary>
public sealed class IdAlreadySetException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IdAlreadySetException"/> class.
	/// </summary>
	public IdAlreadySetException() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="IdAlreadySetException"/> class with a specified message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public IdAlreadySetException(string message)
		: base(message) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="IdAlreadySetException"/> class with a specified message
	/// and a reference to the inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="inner">The exception that is the cause of the current exception.</param>
	public IdAlreadySetException(string message, Exception inner)
		: base(message, inner) { }
}
