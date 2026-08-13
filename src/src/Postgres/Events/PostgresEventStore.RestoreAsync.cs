using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.Postgres.Events;

partial class PostgresEventStore<T>
{
	public async Task<bool> RestoreAsync(
		T aggregate,
		EventStoreOperationContext? operationContext,
		CancellationToken cancellationToken = default
	)
	{
		if (aggregate == null)
			throw NullAggregate(aggregate);

		if (!aggregate.Details.IsDeleted)
			throw AggregateNotDeletedException(aggregate.Id());

		operationContext ??= EventStoreOperationContext.DefaultContext();

		Restored restoreAggregateEvent = new()
		{
			Details =
			{
				AggregateVersion = aggregate.Details.CurrentVersion + 1,
				When = DateTimeOffset.UtcNow,
			},
		};
		aggregate.ApplyEvent(restoreAggregateEvent);

		if (aggregate.IsNew())
			return false;

		var result = await SaveCoreAsync(
			aggregate,
			operationContext,
			null,
			null,
			cancellationToken,
			restoreAggregateEvent
		);
		await result.AfterCommitAsync(cancellationToken);
		return result.Result.Saved;
	}
}
