namespace Purview.EventSourcing.SourceGenerator.Common;

public record EventSourcingGeneratorTestOptions : SourceGeneratorTestOptions
{
	public static readonly string[] AggregateGeneratedAttributes =
	[
		"EmbeddedAttribute.g.cs",
		"PropertyAttribute.g.cs",
		"AggregateAttribute.g.cs",
		"SentinelEventAttribute.g.cs",
		"CollectionEventAttribute.g.cs",
		"AggregateDefaultsAttribute.g.cs",
		"EventAttribute.g.cs",
		"MetadataAttribute.g.cs",
		"ComputedAttribute.g.cs",
	];

	public static readonly int AggregateExpectedFileCount = AggregateGeneratedAttributes.Length;

	public static readonly int AggregateExpectedFileCountPlusGen = AggregateExpectedFileCount + 1;

	public static readonly string[] ValueObjectGeneratedAttributes =
	[
		"EmbeddedAttribute.g.cs",
		"ValueObjectDefaultsAttribute.g.cs",
	];

	public static readonly int ValueObjectExpectedFileCount = ValueObjectGeneratedAttributes.Length;

	public static readonly int ValueObjectExpectedFileCountPlusGen = ValueObjectExpectedFileCount + 1;

	public const int HintNameHashHexLength = 16;

	public const string GeneratedSourceFileSuffix = ".g.cs";

	public EventSourcingGeneratorTestOptions()
	{
		DisableSourceGeneratorPropertyName = PropertyLibrary.DisableSourceGenerator;
		ValidateCodeWriterScopes = true;
		AdditionalNamespaces =
		[
			typeof(EventStoreSet<>).Namespace!,
			typeof(Aggregates.AggregateBase).Namespace!,
			typeof(Serialization.ScalarJsonConverterFactory).Namespace!,
			typeof(ValueObjects.IValueObject).Namespace!,
		];
		AdditionalAssemblyTypes = [typeof(Aggregates.IAggregate)];
		AdditionalReferences = [.. TestMetadataReferences.GetAdditionalReferences()];
		ExcludeGeneratedSourceHintNames = [.. AggregateGeneratedAttributes, .. ValueObjectGeneratedAttributes];
		AnalyzerTypes =
		[
			typeof(Analyzers.AggregateDiagnosticAnalyzer),
			typeof(Analyzers.ValueObjectDiagnosticAnalyzer),
		];
	}

	public static new EventSourcingGeneratorTestOptions Default => new();

	public static readonly EventSourcingGeneratorTestOptions NoValidation = new()
	{
		ThrowOnGenerationException = false,
	};
}
