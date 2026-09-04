using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Samples.Domain;

/// <summary>
/// Represents a physical storage facility that can hold inventory.
/// </summary>
[Aggregate]
public sealed partial class LocationAggregate : AggregateBase
{
	public string LocationId { get; private set; } = default!;

	public string LocationName { get; private set; } = default!;

	[Event]
	public partial LocationAggregate Create(string locationId, string locationName);

	[Event]
	public partial LocationAggregate Rename(string locationName);
}
