namespace Purview.EventSourcing.SourceGenerator.Helpers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public static class TypeLibrary
{
	public const string AggregateNamespace = "Purview.EventSourcing.Aggregates";

	public const string EventsNamespace = "Purview.EventSourcing.Events";

	public const string SerializationNamespace = "Purview.EventSourcing.Serialization";

	public static class Attributes
	{
		public static readonly IEnumerable<TypeValueObject> GeneratedAttributes = [
			AggregatePropertyAttribute, ComputedAttribute,
			GenerateAggregateAttribute, GenerateCollectionEventAttribute,
			GenerateAggregateDefaultBaseAttribute, GenerateAggregateDefaultsAttribute,
			GenerateEventAttribute, GenerateValueObjectDefaultsAttribute,
			MetadataAttribute];

		public static readonly TypeValueObject AggregatePropertyAttribute = new(nameof(AggregatePropertyAttribute), AggregateNamespace);

		public static readonly TypeValueObject ComputedAttribute = new(nameof(ComputedAttribute), AggregateNamespace);

		public static readonly TypeValueObject GenerateAggregateAttribute = new(nameof(GenerateAggregateAttribute), AggregateNamespace);

		public static readonly TypeValueObject GenerateCollectionEventAttribute = new(nameof(GenerateCollectionEventAttribute), AggregateNamespace);

		public static readonly TypeValueObject GenerateAggregateDefaultBaseAttribute = new(nameof(GenerateAggregateDefaultBaseAttribute), AggregateNamespace);

		public static readonly TypeValueObject GenerateAggregateDefaultsAttribute = new(nameof(GenerateAggregateDefaultsAttribute), AggregateNamespace);

		public static readonly TypeValueObject GenerateEventAttribute = new(nameof(GenerateEventAttribute), AggregateNamespace);

		public static readonly TypeValueObject GenerateValueObjectDefaultsAttribute = new(nameof(GenerateValueObjectDefaultsAttribute), SerializationNamespace);

		public static readonly TypeValueObject MetadataAttribute = new(nameof(MetadataAttribute), AggregateNamespace);

		public static readonly TypeValueObject CollectionEventOperation = new(nameof(CollectionEventOperation), AggregateNamespace);

		public static readonly TypeValueObject ScalarAttribute = new(nameof(ScalarAttribute), SerializationNamespace);
	}

	public static class Aggregates
	{
		public static readonly TypeValueObject AggregateBase = new(nameof(AggregateBase), AggregateNamespace);

		public static readonly TypeValueObject IAggregate = new(nameof(IAggregate), AggregateNamespace);

		public static readonly TypeValueObject EventBase = new(nameof(EventBase), AggregateNamespace);

		public static readonly TypeValueObject IEvent = new(nameof(IEvent), AggregateNamespace);

		public static readonly TypeValueObject EventStoreList = new(nameof(EventStoreList), AggregateNamespace);

		public static readonly TypeValueObject EventStoreSet = new(nameof(EventStoreSet), AggregateNamespace);

	}
}
