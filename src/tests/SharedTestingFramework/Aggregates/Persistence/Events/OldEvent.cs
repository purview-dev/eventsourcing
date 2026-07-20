using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.Aggregates.Persistence.Events;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Purview.EventSourcing.SourceGenerator",
	"EVENTSTORE013:Event names should be past tense",
	Justification = "Testing"
)]
public class OldEvent : EventBase
{
	public Guid Value { get; set; }

	protected override void BuildEventHash(ref HashCode hash)
	{
		hash.Add(Value);
	}
}
