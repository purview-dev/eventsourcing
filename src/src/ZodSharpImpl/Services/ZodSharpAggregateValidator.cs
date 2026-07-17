using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Services;
using Purview.EventSourcing.Validation;
using ZodSharp.Core;

namespace Purview.EventSourcing.ZodSharp.Services;

public sealed class ZodSharpAggregateValidator<TAggregate>(IZodSchemaValidator<TAggregate> validator)
	: IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	public ValidationResult Validate(TAggregate aggregate)
	{
		throw new NotImplementedException();
	}

	public Task<ValidationResult> ValidateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}
}
