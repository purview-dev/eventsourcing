namespace Purview.EventSourcing.Aggregates.Snapshotting;

/// <summary>
/// Resolves and evaluates snapshot strategies with the precedence:
/// operation context, selector, then default strategy.
/// </summary>
public static class SnapshotStrategyResolver
{
	/// <summary>
	/// Resolves the snapshot strategy for an aggregate, honoring the operation context override first, then
	/// the selector, and finally falling back to the default strategy.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="context">The operation context that may carry a strategy override or selector.</param>
	/// <param name="defaultStrategy">The default strategy to use when nothing else is configured.</param>
	/// <param name="selector">An optional selector used to choose a strategy.</param>
	/// <returns>The resolved <see cref="ISnapshotStrategy{T}"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="defaultStrategy"/> is null.</exception>
	public static ISnapshotStrategy<T> ResolveStrategy<T>(
		EventStoreOperationContext? context,
		ISnapshotStrategy<T> defaultStrategy,
		ISnapshotStrategySelector? selector = null
	)
		where T : class, IAggregate, new()
	{
		ArgumentNullException.ThrowIfNull(defaultStrategy);

		if (context?.TryGetSnapshotStrategy<T>(out var contextStrategy) == true)
			return contextStrategy!;

		var selectedStrategy = context?.SnapshotStrategySelector?.Resolve<T>() ?? selector?.Resolve<T>();

		return selectedStrategy ?? defaultStrategy;
	}

	/// <summary>
	/// Determines whether a snapshot should be taken for the aggregate given the number of events applied.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="aggregate">The aggregate being evaluated.</param>
	/// <param name="eventsApplied">The number of events applied in the current operation.</param>
	/// <param name="context">The operation context that may carry a strategy override or selector.</param>
	/// <param name="defaultStrategy">The default strategy to use when nothing else is configured.</param>
	/// <param name="selector">An optional selector used to choose a strategy.</param>
	/// <returns>True when the resolved strategy indicates a snapshot should be taken.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is null.</exception>
	public static bool ShouldSnapshot<T>(
		T aggregate,
		int eventsApplied,
		EventStoreOperationContext? context,
		ISnapshotStrategy<T> defaultStrategy,
		ISnapshotStrategySelector? selector = null
	)
		where T : class, IAggregate, new()
	{
		ArgumentNullException.ThrowIfNull(aggregate);

		var strategy = ResolveStrategy(context, defaultStrategy, selector);
		var savedVersion = aggregate.Details.SavedVersion;
		try
		{
			aggregate.Details.SavedVersion = aggregate.Details.CurrentVersion;
			return strategy.ShouldSnapshot(aggregate, eventsApplied);
		}
		finally
		{
			aggregate.Details.SavedVersion = savedVersion;
		}
	}
}
