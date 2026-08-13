namespace Purview.EventSourcing.SourceGenerator.Helpers;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible"
)]
public static class TypeLibrary
{
	public const string AggregateNamespace = "Purview.EventSourcing.Aggregates";

	public const string EventsNamespace = "Purview.EventSourcing.Aggregates.Events";

	public const string SerializationNamespace = "Purview.EventSourcing.Serialization";

	public const string CollectionsNamespace = "Purview.EventSourcing";

	public static class Attributes
	{
		public static readonly TypeValueObject PropertyAttribute = new(
			nameof(PropertyAttribute),
			AggregateNamespace
		);

		public static readonly TypeValueObject ComputedAttribute = new(
			nameof(ComputedAttribute),
			AggregateNamespace
		);

		public static readonly TypeValueObject AggregateAttribute = new(
			nameof(AggregateAttribute),
			AggregateNamespace
		);

		public static readonly TypeValueObject CollectionEventAttribute = new(
			nameof(CollectionEventAttribute),
			AggregateNamespace
		);

		public static readonly TypeValueObject AggregateDefaultsAttribute = new(
			nameof(AggregateDefaultsAttribute),
			AggregateNamespace
		);

		public static readonly TypeValueObject EventAttribute = new(
			nameof(EventAttribute),
			AggregateNamespace
		);

		public static readonly TypeValueObject ValueObjectDefaultsAttribute = new(
			nameof(ValueObjectDefaultsAttribute),
			SerializationNamespace
		);

		public static readonly TypeValueObject MetadataAttribute = new(
			nameof(MetadataAttribute),
			AggregateNamespace
		);

		public static readonly TypeValueObject CollectionEventOperation = new(
			nameof(CollectionEventOperation),
			AggregateNamespace
		);

		public static readonly TypeValueObject ScalarAttribute = new(
			nameof(ScalarAttribute),
			SerializationNamespace
		);

		public static readonly IEnumerable<TypeValueObject> GeneratedAttributes =
		[
			PropertyAttribute,
			ComputedAttribute,
			AggregateAttribute,
			CollectionEventAttribute,
			AggregateDefaultsAttribute,
			EventAttribute,
			MetadataAttribute,
		];
	}

	public static class Aggregates
	{
		public static readonly TypeValueObject AggregateBase = new(
			nameof(AggregateBase),
			AggregateNamespace
		);

		public static readonly TypeValueObject IAggregate = new(
			nameof(IAggregate),
			AggregateNamespace
		);

		public static readonly TypeValueObject EventBase = new(nameof(EventBase), EventsNamespace);

		public static readonly TypeValueObject IEvent = new(nameof(IEvent), EventsNamespace);

		public static readonly TypeValueObject EventStoreList = new TypeValueObject(
			nameof(EventStoreList),
			CollectionsNamespace
		)
		{
			GenericArity = 1,
		};

		public static readonly TypeValueObject EventStoreSet = new TypeValueObject(
			nameof(EventStoreSet),
			CollectionsNamespace
		)
		{
			GenericArity = 1,
		};
	}
}
