namespace Purview.EventSourcing.Validation;

/// <summary>
/// Represents a single validation failure.
/// </summary>
/// <param name="PropertyName">
/// The property that failed validation, or <see langword="null"/> for an
/// aggregate-wide failure.
/// </param>
/// <param name="ErrorMessage">The error message describing the failure.</param>
public sealed record ValidationFailure(string? PropertyName, string ErrorMessage)
{
	/// <summary>
	/// <see langword="true"/> if this failure is an aggregate-wide failure (not tied to a specific property);
	/// </summary>
	public bool IsAggregateFailure => PropertyName is null;
}
