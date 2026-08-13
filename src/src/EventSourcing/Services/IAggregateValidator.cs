using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Validation;

namespace Purview.EventSourcing.Services;

public interface IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	ValidationResult Validate(TAggregate aggregate);

	Task<ValidationResult> ValidateAsync(
		TAggregate aggregate,
		CancellationToken cancellationToken = default
	);
}
