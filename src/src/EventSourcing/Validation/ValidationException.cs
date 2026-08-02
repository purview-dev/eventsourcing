namespace Purview.EventSourcing.Validation;

/// <summary>
/// Thrown when aggregate validation fails (e.g. via <see cref="Aggregates.SaveResult{TAggregate}.EnsureValid"/>).
/// </summary>
public sealed class ValidationException : Exception
{
	/// <summary>
	/// The validation failures that caused this exception.
	/// </summary>
	public IReadOnlyList<ValidationFailure> Errors { get; }

	/// <summary>
	/// Constructs a new <see cref="ValidationException"/>.
	/// </summary>
	public ValidationException()
		: base("Validation failed.")
	{
		Errors = [];
	}

	/// <summary>
	/// Constructs a new <see cref="ValidationException"/> with the given failures.
	/// </summary>
	public ValidationException(IEnumerable<ValidationFailure> errors)
		: base("Validation failed: " + string.Join("; ", errors.Select(e => e.ErrorMessage)))
	{
		Errors = [.. errors];
	}

	/// <summary>
	/// Constructs a new <see cref="ValidationException"/> with the given message and failures.
	/// </summary>
	public ValidationException(string message, IEnumerable<ValidationFailure> errors)
		: base(message)
	{
		Errors = [.. errors];
	}

	/// <summary>
	/// Constructs a new <see cref="ValidationException"/> with the given message.
	/// </summary>
	public ValidationException(string message)
		: base(message)
	{
		Errors = [];
	}

	/// <summary>
	/// Constructs a new <see cref="ValidationException"/> with the given message and inner exception.
	/// </summary>
	public ValidationException(string message, Exception innerException)
		: base(message, innerException)
	{
		Errors = [];
	}
}
