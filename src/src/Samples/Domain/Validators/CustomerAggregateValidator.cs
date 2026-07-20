using FluentValidation;

namespace Purview.EventSourcing.Samples.Domain.Validators;

public sealed class CustomerAggregateValidator : AbstractValidator<CustomerAggregate>
{
	public CustomerAggregateValidator()
	{
		RuleFor(m => m.PhoneNumber).MaximumLength(20);
	}
}
