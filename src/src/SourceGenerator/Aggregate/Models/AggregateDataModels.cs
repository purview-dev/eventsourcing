using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

[Generate(TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.AggregateAttribute))]
readonly partial record struct AggregateAttributeData(string? EventNamespace, string? EventSuffix);

[Generate(
	TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.AggregateDefaultsAttribute)
)]
readonly partial record struct AggregateDefaultsAttributeData(
	string? EventSuffix,
	ITypeSymbol? EventType
);

[Generate(
	TypeLibrary.AggregateNamespace + "." + nameof(TypeLibrary.Attributes.CollectionEventAttribute)
)]
readonly partial record struct CollectionEventAttributeData(
	[Argument("propertyName")] string PropertyName,
	[Property(1)] int Version,
	string? EventName,
	string? EventNamespace,
	[Property(
		TypeLibrary.AggregateNamespace
			+ "."
			+ nameof(TypeLibrary.Attributes.CollectionEventOperation)
			+ ".Auto",
		IsEnum = true
	)]
		string Operation,
	[Property(true)] bool Manual
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
readonly partial record struct PropertyAttributeData(
	[Argument("propertyName")] string PropertyName
);
