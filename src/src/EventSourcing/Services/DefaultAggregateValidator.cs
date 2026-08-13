using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Validation;

namespace Purview.EventSourcing.Services;

/// <summary>
/// A default validator for <see cref="IAggregate"/>'s based on
/// standard data annotations.
/// </summary>
public sealed class DefaultAggregateValidator<TAggregate> : IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	/// <summary>
	/// A statically cached instance based on the use of standard data annotations.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Design",
		"CA1000:Do not declare static members on generic types"
	)]
	public static IAggregateValidator<TAggregate> Instance { get; } =
		new DefaultAggregateValidator<TAggregate>();

	public ValidationResult Validate(TAggregate aggregate)
	{
		ArgumentNullException.ThrowIfNull(aggregate);

		var failures = ValidateWithAnnotations(aggregate);
		return new ValidationResult(failures);
	}

	public Task<ValidationResult> ValidateAsync(
		TAggregate aggregate,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(aggregate);

		var failures = ValidateWithAnnotations(aggregate);
		return Task.FromResult(new ValidationResult(failures));
	}

	static IEnumerable<ValidationFailure> ValidateWithAnnotations(TAggregate aggregate)
	{
		System.ComponentModel.DataAnnotations.ValidationContext daContext = new(aggregate);
		List<System.ComponentModel.DataAnnotations.ValidationResult> failures = [];

		if (
			!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
				aggregate,
				daContext,
				failures,
				true
			)
		)
		{
			foreach (var failure in failures)
			{
				foreach (var memberName in failure.MemberNames)
					yield return new ValidationFailure(
						memberName,
						failure.ErrorMessage ?? "Validation failed (no error provided)"
					);
			}
		}
	}
}
