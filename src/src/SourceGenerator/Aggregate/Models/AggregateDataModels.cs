namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

[Generate(TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.AggregateAttribute))]
readonly partial record struct AggregateAttributeData(string? EventNamespace, string? EventSuffix);

[Generate(TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.AggregateDefaultsAttribute))]
readonly partial record struct AggregateDefaultsAttributeData(string? EventSuffix, TypeIdentity EventType);

[Generate(TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.CollectionEventAttribute))]
readonly partial record struct CollectionEventAttributeData(
	[Argument("propertyName", defaultValue: "")] string PropertyName,
	[Property(1)] int Version,
	string? EventName,
	string? EventNamespace,
	[Property("Auto", IsEnum = true)] string Operation,
	bool Manual
);

[Generate(TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.EventAttribute))]
readonly partial record struct EventAttributeData(
	[Property(1)] int Version,
	string? EventName,
	string? EventNamespace,
	bool Manual
);

[Generate(TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.MetadataAttribute))]
readonly partial record struct MetadataAttributeData([Argument("store", true)] bool Store);

[Generate(TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.PropertyAttribute))]
readonly partial record struct PropertyAttributeData([Argument("propertyName", defaultValue: "")] string PropertyName);
