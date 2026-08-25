namespace Purview.EventSourcing.SourceGenerator.Common;

partial class TypeLibrary
{
	public static class Attributes
	{
		public const string ValueObjectDefaultsAttributeFullTypeName =
			SerializationNamespace + ".ValueObjectDefaultsAttribute";

		public const string ValueObjectAttributeFullTypeName = SerializationNamespace + ".ValueObjectAttribute";

		public const string ScalarAttributeFullTypeName = SerializationNamespace + ".ScalarAttribute";

		public const string ValueObjectDeserializationModeFullTypeName =
			SerializationNamespace + ".ValueObjectDeserializationMode";

		public static readonly TypeIdentity PropertyAttribute = new(nameof(PropertyAttribute), AggregateNamespace);

		public static readonly TypeIdentity ComputedAttribute = new(nameof(ComputedAttribute), AggregateNamespace);

		public static readonly TypeIdentity AggregateAttribute = new(nameof(AggregateAttribute), AggregateNamespace);

		public static readonly TypeIdentity SentinelEventAttribute = new(
			nameof(SentinelEventAttribute),
			AggregateNamespace
		);

		public static readonly TypeIdentity CollectionEventAttribute = new(
			nameof(CollectionEventAttribute),
			AggregateNamespace
		);

		public static readonly TypeIdentity AggregateDefaultsAttribute = new(
			nameof(AggregateDefaultsAttribute),
			AggregateNamespace
		);

		public static readonly TypeIdentity EventAttribute = new(nameof(EventAttribute), AggregateNamespace);

		public static readonly TypeIdentity ValueObjectDefaultsAttribute = new(
			nameof(ValueObjectDefaultsAttribute),
			SerializationNamespace
		);

		public static readonly TypeIdentity MetadataAttribute = new(nameof(MetadataAttribute), AggregateNamespace);

		public static readonly TypeIdentity CollectionEventOperation = new(
			nameof(CollectionEventOperation),
			AggregateNamespace
		);

		public static readonly TypeIdentity ScalarAttribute = new(nameof(ScalarAttribute), SerializationNamespace);
	}
}
