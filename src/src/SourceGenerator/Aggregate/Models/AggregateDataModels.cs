using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Generators;

namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

[Generate("Purview.EventSourcing.Aggregates.AggregateAttribute")]
readonly partial record struct AggregateAttributeData(string? EventNamespace, string? EventSuffix);

[Generate("Purview.EventSourcing.Aggregates.AggregateDefaultsAttribute")]
readonly partial record struct AggregateDefaultsAttributeData(
	[Property(DefaultValue = "Event")] string? EventSuffix,
	ITypeSymbol? BaseType
);

[Generate("Purview.EventSourcing.Aggregates.CollectionEventAttribute")]
readonly partial record struct CollectionEventAttributeData(
	[Argument("propertyName")] string PropertyName,
	[Property(DefaultValue = 1)] int Version,
	string? EventName,
	string? EventNamespace,
	[Property(
		DefaultValue = "Purview.EventSourcing.Aggregates.CollectionEventOperation.Auto",
		IsEnum = true
	)]
		string Operation,
	bool Manual
);

[Generate("Purview.EventSourcing.Aggregates.EventAttribute")]
readonly partial record struct EventAttributeData(
	[Property(DefaultValue = 1)] int Version,
	string? EventName,
	string? EventNamespace,
	bool Manual
);

[Generate("Purview.EventSourcing.Aggregates.MetadataAttribute")]
readonly partial record struct MetadataAttributeData(
	[Argument("store", DefaultValue = true)] bool Store
);

[Generate("Purview.EventSourcing.Aggregates.PropertyAttribute")]
readonly partial record struct PropertyAttributeData(
	[Argument("propertyName")] string PropertyName
);
