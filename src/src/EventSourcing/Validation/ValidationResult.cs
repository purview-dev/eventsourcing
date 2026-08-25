using System.Collections.Immutable;

namespace Purview.EventSourcing.Validation;

/// <summary>
/// Represents the result of validating an aggregate.
/// </summary>
public sealed class ValidationResult
{
	/// <summary>
	/// A static instance representing successful validation with no failures.
	/// </summary>
	public static ValidationResult Success { get; } = new();

	/// <summary>
	/// Constructs a successful validation result.
	/// </summary>
	public ValidationResult()
	{
		Failures = [];
	}

	/// <summary>
	/// Constructs a validation result containing the specified failures.
	/// </summary>
	/// <param name="failures">The validation failures.</param>
	public ValidationResult(ImmutableArray<ValidationFailure> failures)
	{
		Failures = failures.IsDefault ? [] : failures;
	}

	/// <summary>
	/// Constructs a validation result containing the specified failures.
	/// </summary>
	/// <param name="failures">The validation failures.</param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="failures"/> is <see langword="null"/>.
	/// </exception>
	public ValidationResult(IEnumerable<ValidationFailure> failures)
	{
		ArgumentNullException.ThrowIfNull(failures);

		Failures = [.. failures];
	}

	/// <summary>
	/// <see langword="true"/> when there are no validation failures;
	/// otherwise <see langword="false"/>.
	/// </summary>
	public bool IsValid => Failures.IsEmpty;

	/// <summary>
	/// <see langword="true"/> when there are one or more validation failures;
	/// otherwise <see langword="false"/>.
	/// </summary>
	public bool HasFailures => !Failures.IsEmpty;

	/// <summary>
	/// <see langword="true"/> when there are one or more aggregate-wide validation failures;
	/// </summary>
	public bool HasAggregateFailures => Failures.Any(f => f.IsAggregateFailure);

	/// <summary>
	/// <see langword="true"/> when there are one or more property-specific validation failures;
	/// </summary>
	public bool HasPropertyFailures => Failures.Any(f => !f.IsAggregateFailure);

	/// <summary>
	/// The validation failures produced during validation.
	/// </summary>
	public ImmutableArray<ValidationFailure> Failures { get; }
}
