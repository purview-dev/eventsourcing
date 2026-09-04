using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Validation;

namespace Purview.EventSourcing.Storage;

/// <summary>
/// Builds <see cref="SaveResult{TAggregate}"/> instances for event-store save operations,
/// centralizing the construction that was previously duplicated across each storage provider.
/// </summary>
public static class SaveResultBuilder
{
	/// <summary>
	/// Creates a <see cref="SaveResult{TAggregate}"/>, defaulting the validation result to a
	/// successful <see cref="ValidationResult"/> when one is not supplied.
	/// </summary>
	public static SaveResult<TAggregate> Create<TAggregate>(
		TAggregate aggregate,
		bool saved,
		bool skipped,
		ValidationResult? validationResult = null
	)
		where TAggregate : IAggregate => new(aggregate, validationResult ?? new ValidationResult(), saved, skipped);
}
