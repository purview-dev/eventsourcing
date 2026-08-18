namespace Purview.EventSourcing.SourceGenerator.ValueObject.Models;

[Generate("Purview.EventSourcing.Serialization.ScalarAttribute")]
readonly partial record struct ScalarAttributeData(
	[Argument("propertyName", DefaultValue = "Value")] string PropertyName,
	[Property(DefaultValue = true)] bool GenerateJsonConverter,
	[Property(DefaultValue = true)] bool GenerateComparable,
	[Property(DefaultValue = true)] bool GenerateComparisonOperators,
	[Property(DefaultValue = true)] bool GenerateEnumProperties,
	[Property(DefaultValue = true)] bool GenerateImplicitFromPrimitive,
	[Property(DefaultValue = true)] bool GenerateImplicitToPrimitive,
	[Property(DefaultValue = true)] bool GenerateEmpty,
	[Property(
		DefaultValue = "global::Purview.EventSourcing.Serialization.ValueObjectDeserializationMode.Hydrate",
		IsEnum = true
	)]
		string DeserializationMode
);

[Generate("Purview.EventSourcing.Serialization.ValueObjectAttribute")]
readonly partial record struct ValueObjectAttributeData(
	[Property(DefaultValue = true)] bool GenerateJsonConverter,
	[Property(DefaultValue = true)] bool GenerateComparable,
	[Property(DefaultValue = true)] bool GenerateComparisonOperators,
	[Property(DefaultValue = true)] bool GenerateEmpty,
	[Property(DefaultValue = true)] bool GenerateConstructor,
	[Property(
		DefaultValue = "global::Purview.EventSourcing.Serialization.ValueObjectDeserializationMode.Hydrate",
		IsEnum = true
	)]
		string DeserializationMode
);

[Generate("Purview.EventSourcing.Serialization.ValueObjectDefaultsAttribute")]
readonly partial record struct ValueObjectDefaultsAttributeData(
	[Property(DefaultValue = true)] bool GenerateConstructor
);
