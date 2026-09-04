using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.Aggregates.Persistence.Events;

[SentinelEvent(Justification = "This is an old event used for testing purposes.")]
public sealed class OldEvent : EventBase
{
	public Guid Value { get; set; }

	protected override void BuildEventHash(ref HashCode hash)
	{
		hash.Add(Value);
	}
}
