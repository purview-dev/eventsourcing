namespace Purview.EventSourcing.Validation;

/// <summary>
/// Represents a single validation failure.
/// </summary>
public sealed record ValidationFailure
{
	/// <summary>
	/// The name of the property that failed validation, or <see langword="null"/> if the failure is aggregate-wide.
	/// </summary>
	public string? PropertyName { get; init; }

	/// <summary>
	/// The error message describing the failure.
	/// </summary>
	public string? ErrorMessage { get; init; }

	/// <summary>
	/// Constructs a new <see cref="ValidationFailure"/>.
	/// </summary>
	public ValidationFailure(string? propertyName, string? errorMessage)
	{
		PropertyName = propertyName;
		ErrorMessage = errorMessage;
	}
}
