using System.Collections.Concurrent;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Samples.QuickStart.Infrastructure;

sealed class InMemoryFailurePlan
{
	readonly ConcurrentDictionary<(Type AggregateType, string AggregateId), byte> _failNextSave =
		new();

	public void FailNextSave<TAggregate>(string aggregateId)
		where TAggregate : IAggregate => _failNextSave.TryAdd((typeof(TAggregate), aggregateId), 0);

	public bool ShouldFail(Type aggregateType, string aggregateId) =>
		_failNextSave.TryRemove((aggregateType, aggregateId), out _);
}
