using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Services;
using Purview.EventSourcing.Validation;
using ZodSharp.Core;

namespace Purview.EventSourcing.ZodSharp.Services;

public sealed class ZodSharpAggregateValidator<TAggregate>(IZodSchemaValidator<TAggregate> validator)
	: IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	public ValidationResult Validate(TAggregate aggregate) => Convert(validator.Validate(aggregate));

	public async Task<ValidationResult> ValidateAsync(
		TAggregate aggregate,
		CancellationToken cancellationToken = default
	)
	{
		var result = await validator.ValidateAsync(aggregate, cancellationToken);

		return Convert(result);
	}

	static ValidationResult Convert(ValidationResult<TAggregate> validationResult) =>
		validationResult.IsSuccess
			? ValidationResult.Success
			: new ValidationResult(
				validationResult.Errors.Select(e => new ValidationFailure(string.Join('.', e.Path), e.Message))
			);
}
