namespace Purview.EventSourcing.Contracts;

/// <summary>
/// Shared data sources used by the event-store and snapshot-store contract suites.
/// This file is linked (not compiled) into each provider integration test project.
/// </summary>
public static class EventStoreContractTestData
{
	public static IEnumerable<int> SteppedCount()
	{
		yield return 1;
		yield return 10;
		yield return 20;
		yield return 50;
	}

	public static IEnumerable<int> SnapshotEventCount()
	{
		yield return 10;
		yield return 20;
		yield return 50;
		yield return 80;
		yield return 100;
	}

	public static IEnumerable<(int EventsToCreate, int NumberOfOldEventsToCreate)> SteppedEventCountWithOldEventCount()
	{
		yield return (1, 1);
		yield return (5, 2);
		yield return (10, 5);
		yield return (20, 20);
	}

	public static IEnumerable<(int EventsToCreate, int StartEvent, int? EndEvent)> RequestedRangeOfEvents()
	{
		yield return (5, 1, 5);
		yield return (5, 1, null);
		yield return (10, 2, 5);
		yield return (10, 2, null);
		yield return (15, 15, null);
		yield return (15, 15, 15);
		// Larger request than actual events exist.
		yield return (5, 1, 20);
		yield return (5, 1, 20000);
	}

	public static IEnumerable<(
		int EventsToCreate,
		int StartEvent,
		int? EndEvent,
		int ExpectedEventCount
	)> RequestedRangeOfEventsWithExpectedEventCount()
	{
		yield return (5, 1, 5, 5);
		yield return (5, 1, null, 5);
		yield return (10, 2, 5, 4);
		yield return (10, 2, null, 9);
		yield return (15, 15, null, 1);
		yield return (15, 15, 15, 1);
		// Larger request than actual events exist.
		yield return (5, 1, 20, 5);
		yield return (5, 1, 20000, 5);
	}

	public static IEnumerable<(
		int NonDeletedAggregateCount,
		int DeletedAggregateCount
	)> SteppedAggregateCountWithDeletedAggregateCount()
	{
		yield return (1, 1);
		yield return (1, 10);
		yield return (5, 5);
		yield return (5, 10);
		yield return (10, 10);
		yield return (10, 20);
		yield return (20, 20);
		yield return (20, 40);
	}

	public static IEnumerable<int> TooManyEventCount()
	{
		yield return 1_001;
		yield return 10_000;
		yield return 100_000;
	}
}
