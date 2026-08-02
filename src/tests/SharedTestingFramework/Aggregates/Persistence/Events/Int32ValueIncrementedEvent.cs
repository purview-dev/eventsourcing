using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.Aggregates.Persistence.Events;

public class Int32ValueIncrementedEvent : EventBase
{
	protected override void BuildEventHash(ref HashCode hash) { }
}
