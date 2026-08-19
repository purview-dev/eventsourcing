namespace Purview.EventSourcing.SourceGenerator.Common;

partial class TypeLibrary
{
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

		public static readonly TypeValueObject SentinelEventAttribute = new(
			nameof(SentinelEventAttribute),
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
	}
}
