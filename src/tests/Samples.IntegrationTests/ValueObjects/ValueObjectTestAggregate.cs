using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Samples.ValueObjects;

[Aggregate]
sealed partial class ValueObjectTestAggregate
{
	public UserCapture UserCapture { get; private set; }

	[Event]
	public partial void SetUserCapture(UserCapture userCapture);
}
