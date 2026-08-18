using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Common;

public abstract class EventSourcingSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, EventSourcingGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new()
{
	readonly SourceGeneratorTestRunner<TGenerator> _eventSourcingRunner = new();

	protected new Task<DriverRunResult> GenerateAsync(
		string source,
		CancellationToken cancellationToken = default
	) =>
		_eventSourcingRunner.RunAsync(
			source,
			new EventSourcingGeneratorTestOptions(),
			cancellationToken
		);

	protected new Task<DriverRunResult> GenerateAsync(
		string source,
		EventSourcingGeneratorTestOptions options,
		CancellationToken cancellationToken = default
	) => _eventSourcingRunner.RunAsync(source, options, cancellationToken);

	protected const int HintNameHashHexLength =
		EventSourcingGeneratorTestOptions.HintNameHashHexLength;

	protected const string GeneratedSourceFileSuffix =
		EventSourcingGeneratorTestOptions.GeneratedSourceFileSuffix;

	protected static int ExpectedFileCount => EventSourcingGeneratorTestOptions.ExpectedFileCount;

	protected static int ExpectedFileCountPlusGen =>
		EventSourcingGeneratorTestOptions.ExpectedFileCountPlusGen;

	protected async Task<Assembly> CompileToAssemblyAsync(
		string source,
		CancellationToken cancellationToken
	)
	{
		var options = new EventSourcingGeneratorTestOptions { CompileToAssembly = true };
		var result = await GenerateAsync(source, options, cancellationToken);

		return result.CompilationResult.Assembly
			?? throw new InvalidOperationException(
				"The generated source did not compile to an assembly."
			);
	}

	protected static IEnumerable<SyntaxTree> ExcludeGenAttribs(DriverRunResult result) =>
		result.DriverResult.GeneratedTrees.Where(tree =>
			!EventSourcingGeneratorTestOptions.GeneratedAttributes.Any(attribute =>
				tree.FilePath.Contains(
					Path.GetFileNameWithoutExtension(attribute),
					StringComparison.Ordinal
				)
			)
		);
}
