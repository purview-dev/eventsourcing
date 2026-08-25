namespace Purview.EventSourcing.SourceGenerator.Common;

partial class TypeLibrary
{
	public static class Aggregates
	{
		public static readonly TypeIdentity AggregateBase = new(nameof(AggregateBase), AggregateNamespace);

		public static readonly TypeIdentity AggregateDetails = new(nameof(AggregateDetails), AggregateNamespace);

		public static readonly TypeIdentity IAggregate = new(nameof(IAggregate), AggregateNamespace);

		public static readonly TypeIdentity EventBase = new(nameof(EventBase), EventsNamespace);

		public static readonly TypeIdentity IEvent = new(nameof(IEvent), EventsNamespace);

		public static readonly TypeIdentity EventStoreList = new(nameof(EventStoreList), CollectionsNamespace)
		{
			GenericArity = 1,
		};

		public static readonly TypeIdentity EventStoreSet = new(nameof(EventStoreSet), CollectionsNamespace)
		{
			GenericArity = 1,
		};
	}
}
