using FluentValidation;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Services;
using Purview.EventSourcing.Validation;

namespace Purview.EventSourcing.FluentValidation.Services;

/// <summary>
/// Adapts a <see cref="IValidator{TAggregate}"/> to the
/// <see cref="IAggregateValidator{TAggregate}"/> interface, mapping FluentValidation
/// results to <see cref="ValidationResult"/>.
/// </summary>
public sealed class FluentValidationAggregateValidator<TAggregate>(IValidator<TAggregate> validator)
	: IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	readonly IValidator<TAggregate> _validator = validator;

	public ValidationResult Validate(TAggregate aggregate) => Map(_validator.Validate(aggregate));

	public async Task<ValidationResult> ValidateAsync(
		TAggregate aggregate,
		CancellationToken cancellationToken = default
	)
	{
		var result = await _validator.ValidateAsync(aggregate, cancellationToken);
		return Map(result);
	}

	static ValidationResult Map(global::FluentValidation.Results.ValidationResult result)
	{
		if (result.IsValid)
			return ValidationResult.Success;

		var failures = result.Errors.Select(e => new ValidationFailure(
			e.PropertyName,
			e.ErrorMessage
		));
		return new ValidationResult(failures);
	}
}
