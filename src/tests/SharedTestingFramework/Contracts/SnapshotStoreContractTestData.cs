namespace Purview.EventSourcing.Contracts;

/// <summary>
/// Shared data sources used by the snapshot-store contract suite.
/// This file is linked (not compiled) into each provider integration test project.
/// </summary>
public static class SnapshotStoreContractTestData
{
	public static IEnumerable<(int NumberOfAggregates, int NumberOfEvents)> AggregateAndEventCounts()
	{
		yield return (1, 1);
		yield return (1, 5);
		yield return (1, 10);
		yield return (5, 1);
		yield return (5, 5);
		yield return (5, 10);
		yield return (10, 1);
		yield return (10, 5);
		yield return (10, 10);
	}

	public static IEnumerable<int> AggregateCounts()
	{
		yield return 1;
		yield return 5;
		yield return 10;
	}

	public static IEnumerable<(int NumberOfAggregates, int PageCount)> PageSizeData()
	{
		yield return (10, 5);
		yield return (20, 5);
		yield return (25, 5);
		yield return (26, 5);
		yield return (27, 5);
		yield return (50, 5);
		yield return (51, 5);
	}

	public static IEnumerable<int> CountData()
	{
		yield return 1;
		yield return 5;
		yield return 10;
		yield return 25;
	}

	public static IEnumerable<int> EnumerableCountData()
	{
		yield return 1;
		yield return 5;
		yield return 10;
	}
}
