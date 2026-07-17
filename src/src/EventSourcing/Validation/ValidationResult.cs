namespace Purview.EventSourcing.Validation;

/// <summary>
/// Represents the result of validating an aggregate.
/// </summary>
/// <remarks>
/// Constructs a new <see cref="ValidationResult"/> with the given failures.
/// </remarks>
/// <param name="failures">The validation failures. Pass an empty collection for a successful result.</param>
public sealed class ValidationResult(IEnumerable<ValidationFailure> failures)
{
	/// <summary>
	/// A static instance representing a successful validation with no errors.
	/// </summary>
	public static ValidationResult Success { get; } = new([]);

	/// <summary>
	/// Constructs a new successful <see cref="ValidationResult"/> with no errors.
	/// </summary>
	public ValidationResult()
		: this([]) { }

	/// <summary>
	/// <see langword="true"/> when there are no <see cref="Errors"/>; otherwise <see langword="false"/>.
	/// </summary>
	public bool IsValid => Errors.Count == 0;

	/// <summary>
	/// The collection of <see cref="ValidationFailure"/>s produced during validation.
	/// </summary>
	public IReadOnlyList<ValidationFailure> Errors { get; } = [.. failures];
}
