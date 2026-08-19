namespace Purview.EventSourcing.SourceGenerator.Common;

public record EventSourcingGeneratorTestOptions : SourceGeneratorTestOptions
{
	public static readonly string[] GeneratedAttributes =
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

	public static readonly int ExpectedFileCount = GeneratedAttributes.Length;

	public static readonly int ExpectedFileCountPlusGen = ExpectedFileCount + 1;

	public const int HintNameHashHexLength = 16;

	public const string GeneratedSourceFileSuffix = ".g.cs";

	public EventSourcingGeneratorTestOptions()
	{
		DisableSourceGeneratorPropertyName = PropertyLibrary.DisableSourceGenerator;
		AdditionalNamespaces =
		[
			"Purview.EventSourcing.Aggregates",
			"Purview.EventSourcing.Serialization",
			"Purview.EventSourcing.ValueObjects",
		];
		AdditionalAssemblyTypes = [typeof(Aggregates.IAggregate)];
		AdditionalReferences = [.. TestMetadataReferences.GetAdditionalReferences()];
		ExcludeGeneratedSourceHintNames = [.. GeneratedAttributes];
	}
}
