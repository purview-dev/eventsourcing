namespace Purview.EventSourcing.SourceGenerator.Common;

[global::System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible"
)]
static partial class TypeLibrary
{
	public const string AggregateNamespace = "Purview.EventSourcing.Aggregates";

	public const string EventsNamespace = "Purview.EventSourcing.Aggregates.Events";

	public const string SerializationNamespace = "Purview.EventSourcing.Serialization";

	public const string CollectionsNamespace = "Purview.EventSourcing";

	public const string AggregateGeneratorName = "Purview.EventSourcing.AggregateSourceGenerator";

	public const string ValueObjectGeneratorName =
		"Purview.EventSourcing.ValueObjectSourceGenerator";
}
