namespace Purview.EventSourcing.SourceGenerator.ValueObject.Models;

[Generate(TypeLibrary.Attributes.ScalarAttributeFullTypeName)]
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
		DefaultValue = TypeLibrary.Attributes.ValueObjectDeserializationModeFullTypeName + ".Hydrate",
		IsEnum = true
	)]
		string DeserializationMode
);

[Generate(TypeLibrary.Attributes.ValueObjectAttributeFullTypeName)]
readonly partial record struct ValueObjectAttributeData(
	[Property(DefaultValue = true)] bool GenerateJsonConverter,
	[Property(DefaultValue = true)] bool GenerateComparable,
	[Property(DefaultValue = true)] bool GenerateComparisonOperators,
	[Property(DefaultValue = true)] bool GenerateEmpty,
	[Property(DefaultValue = true)] bool GenerateConstructor,
	[Property(
		DefaultValue = TypeLibrary.Attributes.ValueObjectDeserializationModeFullTypeName + ".Hydrate",
		IsEnum = true
	)]
		string DeserializationMode
);

[Generate(TypeLibrary.Attributes.ValueObjectDefaultsAttributeFullTypeName)]
readonly partial record struct ValueObjectDefaultsAttributeData(
	[Property(DefaultValue = true)] bool GenerateConstructor
);
