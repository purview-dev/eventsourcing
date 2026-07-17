namespace Purview.EventSourcing.SqlServer.Events;

partial class SqlServerEventStoreTests
{
	public static IEnumerable<Func<(Type, int)>> TooManyEventCountTestData()
	{
		foreach (var aggregateType in GetAggregateTypes())
		{
			yield return () => (aggregateType, 1_001);
			yield return () => (aggregateType, 10_000);
			yield return () => (aggregateType, 100_000);
		}
	}

	public static IEnumerable<Func<(Type, int, int)>> SteppedAggregateCountWithDeletedAggregateIdCountTestData()
	{
		foreach (var aggregateType in GetAggregateTypes())
		{
			yield return () => (aggregateType, 1, 1);
			yield return () => (aggregateType, 1, 10);
			yield return () => (aggregateType, 5, 5);
			yield return () => (aggregateType, 5, 10);
			yield return () => (aggregateType, 10, 10);
			yield return () => (aggregateType, 10, 20);
			yield return () => (aggregateType, 20, 20);
			yield return () => (aggregateType, 20, 40);
		}
	}

	public static IEnumerable<Func<(Type, int, int, int?)>> RequestedRangeOfEventsTestData()
	{
		foreach (var aggregateType in GetAggregateTypes())
		{
			yield return () => (aggregateType, 5, 1, 5);
			yield return () => (aggregateType, 5, 1, null);
			yield return () => (aggregateType, 10, 2, 5);
			yield return () => (aggregateType, 10, 2, null);
			yield return () => (aggregateType, 15, 15, null);
			yield return () => (aggregateType, 15, 15, 15);
			yield return () => (aggregateType, 5, 1, 20);
			yield return () => (aggregateType, 5, 1, 20000);
		}
	}

	public static IEnumerable<Func<(Type, int, int, int?, int)>> RequestedRangeOfEventsWithExpectedEventCountTestData()
	{
		foreach (var aggregateType in GetAggregateTypes())
		{
			yield return () => (aggregateType, 5, 1, 5, 5);
			yield return () => (aggregateType, 5, 1, null, 5);
			yield return () => (aggregateType, 10, 2, 5, 4);
			yield return () => (aggregateType, 10, 2, null, 9);
			yield return () => (aggregateType, 15, 15, null, 1);
			yield return () => (aggregateType, 15, 15, 15, 1);
			yield return () => (aggregateType, 5, 1, 20, 5);
			yield return () => (aggregateType, 5, 1, 20000, 5);
		}
	}

	public static IEnumerable<Func<(Type, int)>> SteppedCountTestData()
	{
		foreach (var aggregateType in GetAggregateTypes())
		{
			yield return () => (aggregateType, 1);
			yield return () => (aggregateType, 10);
			yield return () => (aggregateType, 20);
			yield return () => (aggregateType, 50);
		}
	}

	public static IEnumerable<Func<(Type, int, int)>> SteppedEventCountWithOldEventCountTestData()
	{
		foreach (var aggregateType in GetAggregateTypes())
		{
			yield return () => (aggregateType, 1, 1);
			yield return () => (aggregateType, 5, 2);
			yield return () => (aggregateType, 10, 5);
			yield return () => (aggregateType, 20, 20);
		}
	}

	public static IEnumerable<Func<(Type, int)>> SnapshotEventCountTestData()
	{
		foreach (var aggregateType in GetAggregateTypes())
		{
			yield return () => (aggregateType, 10);
			yield return () => (aggregateType, 20);
			yield return () => (aggregateType, 50);
			yield return () => (aggregateType, 80);
			yield return () => (aggregateType, 100);
		}
	}

	public static IEnumerable<Type> GetAggregateTypes()
	{
		yield return typeof(Aggregates.Persistence.PersistenceAggregate);
	}

	internal ISqlServerEventStoreTests CreateSqlServerStoreTests(Type aggregateType)
	{
		var testType = typeof(GenericSqlServerEventStoreTests<>).MakeGenericType(aggregateType);
		return (ISqlServerEventStoreTests)Activator.CreateInstance(testType, args: [fixture])!;
	}
}
