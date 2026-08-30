using Purview.EventSourcing.SourceGenerator.Generators;

namespace Purview.EventSourcing.SourceGenerator.Common;

public abstract class ValueObjectSourceGeneratorTestBase
	: TUnitSourceGeneratorTestBase<ValueObjectSourceGenerator, EventSourcingGeneratorTestOptions>
{
	protected const int HintNameHashHexLength = EventSourcingGeneratorTestOptions.HintNameHashHexLength;

	protected const string GeneratedSourceFileSuffix = EventSourcingGeneratorTestOptions.GeneratedSourceFileSuffix;

	protected static int ExpectedFileCount => EventSourcingGeneratorTestOptions.ValueObjectExpectedFileCount;

	protected static int ExpectedFileCountPlusGen =>
		EventSourcingGeneratorTestOptions.ValueObjectExpectedFileCountPlusGen;
}
