using Purview.EventSourcing.SourceGenerator.Generators;

namespace Purview.EventSourcing.SourceGenerator.Common;

public abstract class AggregateSourceGeneratorTestBase
	: TUnitSourceGeneratorTestBase<AggregateSourceGenerator, EventSourcingGeneratorTestOptions>
{
	protected const int HintNameHashHexLength = EventSourcingGeneratorTestOptions.HintNameHashHexLength;

	protected const string GeneratedSourceFileSuffix = EventSourcingGeneratorTestOptions.GeneratedSourceFileSuffix;

	protected static int ExpectedFileCount => EventSourcingGeneratorTestOptions.AggregateExpectedFileCount;

	protected static int ExpectedFileCountPlusGen =>
		EventSourcingGeneratorTestOptions.AggregateExpectedFileCountPlusGen;
}
