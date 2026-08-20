namespace Purview.EventSourcing.SourceGenerator.Common;

partial class TypeLibrary
{
	public static class Aggregates
	{
		public static readonly TypeValueObject AggregateBase = new(
			nameof(AggregateBase),
			AggregateNamespace
		);

		public static readonly TypeValueObject AggregateDetails = new(
			nameof(AggregateDetails),
			AggregateNamespace
		);

		public static readonly TypeValueObject IAggregate = new(
			nameof(IAggregate),
			AggregateNamespace
		);

		public static readonly TypeValueObject EventBase = new(nameof(EventBase), EventsNamespace);

		public static readonly TypeValueObject IEvent = new(nameof(IEvent), EventsNamespace);

		public static readonly TypeValueObject EventStoreList = new(
			nameof(EventStoreList),
			CollectionsNamespace
		)
		{
			GenericArity = 1,
		};

		public static readonly TypeValueObject EventStoreSet = new(
			nameof(EventStoreSet),
			CollectionsNamespace
		)
		{
			GenericArity = 1,
		};
	}
}
