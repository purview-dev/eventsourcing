namespace Purview.EventSourcing.Aggregates;

public partial class AggregateBaseTests
{
	[Test]
	public async Task SkippedEvents_GivenNoSkips_ReturnsEmpty()
	{
		// Arrange
		var aggregate = CreateTestAggregate();

		// Act
		var result = aggregate.SkippedEvents;

		// Assert
		await Assert.That(result).IsEmpty();
	}

	[Test]
	public async Task SkippedEvents_GivenSkippedEventsRecorded_ReturnsThemInOrder()
	{
		// Arrange
		var aggregate = CreateTestAggregate();

		// Act
		aggregate.RecordSkippedEvent(1, "orders.order-created-v1", isUnknown: true);
		aggregate.RecordSkippedEvent(2, "orders.order-paid-v2", isUnknown: false);

		// Assert
		await Assert.That(aggregate.SkippedEvents).Count().IsEqualTo(2);
		await Assert.That(aggregate.SkippedEvents[0].AggregateVersion).IsEqualTo(1);
		await Assert.That(aggregate.SkippedEvents[0].EventTypeName).IsEqualTo("orders.order-created-v1");
		await Assert.That(aggregate.SkippedEvents[0].IsUnknown).IsTrue();
		await Assert.That(aggregate.SkippedEvents[1].AggregateVersion).IsEqualTo(2);
		await Assert.That(aggregate.SkippedEvents[1].EventTypeName).IsEqualTo("orders.order-paid-v2");
		await Assert.That(aggregate.SkippedEvents[1].IsUnknown).IsFalse();
	}
}
