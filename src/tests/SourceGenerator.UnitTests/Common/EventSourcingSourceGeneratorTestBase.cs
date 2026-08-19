using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Common;

public abstract class EventSourcingSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, EventSourcingGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new()
{
	protected const int HintNameHashHexLength =
		EventSourcingGeneratorTestOptions.HintNameHashHexLength;

	protected const string GeneratedSourceFileSuffix =
		EventSourcingGeneratorTestOptions.GeneratedSourceFileSuffix;

	protected static int ExpectedFileCount => EventSourcingGeneratorTestOptions.ExpectedFileCount;

	protected static int ExpectedFileCountPlusGen =>
		EventSourcingGeneratorTestOptions.ExpectedFileCountPlusGen;
}
