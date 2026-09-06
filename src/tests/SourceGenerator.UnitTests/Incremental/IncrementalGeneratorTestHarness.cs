using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.EventSourcing.SourceGenerator.Incremental;

static class IncrementalGeneratorTestHarness
{
	public static GeneratorDriver CreateDriver<TGenerator>()
		where TGenerator : class, IIncrementalGenerator, new() =>
		CSharpGeneratorDriver.Create(
			[new TGenerator().AsSourceGenerator()],
			parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
			driverOptions: new GeneratorDriverOptions(
				IncrementalGeneratorOutputKind.None,
				trackIncrementalGeneratorSteps: true
			)
		);

	public static GeneratorDriver CreateDriver<TGenerator>(ImmutableArray<AdditionalText> additionalTexts)
		where TGenerator : class, IIncrementalGenerator, new() =>
		CSharpGeneratorDriver.Create(
			[new TGenerator().AsSourceGenerator()],
			additionalTexts: additionalTexts,
			parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
			driverOptions: new GeneratorDriverOptions(
				IncrementalGeneratorOutputKind.None,
				trackIncrementalGeneratorSteps: true
			)
		);

	public static CSharpCompilation CreateCompilation(IEnumerable<SyntaxTree> trees) =>
		CSharpCompilation.Create(
			"TestAssembly",
			trees,
			ResolveReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

	public static SyntaxTree ParseTree(string source, string filePath) =>
		CSharpSyntaxTree.ParseText(source, path: filePath, options: new CSharpParseOptions(LanguageVersion.Latest));

	public static ImmutableArray<MetadataReference> ResolveReferences()
	{
		var generatorAssemblyPath = typeof(SourceGeneratorFramework.TypeIdentity).Assembly.Location;
		var builder = ImmutableArray.CreateBuilder<MetadataReference>();

		var trusted = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty).Split(
			Path.PathSeparator,
			StringSplitOptions.RemoveEmptyEntries
		);
		foreach (var path in trusted)
		{
			if (string.Equals(path, generatorAssemblyPath, StringComparison.OrdinalIgnoreCase))
				continue;

			builder.Add(MetadataReference.CreateFromFile(path));
		}

		builder.Add(
			MetadataReference.CreateFromFile(
				typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location
			)
		);
		builder.Add(
			MetadataReference.CreateFromFile(typeof(Aggregates.IAggregate).Assembly.Location)
		);
		builder.Add(MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location));

		return builder.ToImmutable();
	}

	public static ImmutableArray<IncrementalGeneratorRunStep> GetSteps(GeneratorRunResult result, string stepName) =>
		result.TrackedSteps.TryGetValue(stepName, out var steps) ? steps : [];

	public static IncrementalStepRunReason GetReason(IncrementalGeneratorRunStep step) =>
		step.Outputs.Length > 0 ? step.Outputs[0].Reason : default;

	public static string GetAggregateName(IncrementalGeneratorRunStep step)
	{
		if (
			step.Outputs.Length > 0
			&& step.Outputs[0].Value
				is SourceGeneratorFramework.GeneratorResult<AggregateTarget> result
		)
		{
			return result.HasValue ? result.Value.Info.AggregateClass.Identity.Name : "(failed)";
		}

		// If the output is not an AggregateTarget, it might be a ValueObjectModel
		return "(unknown)";
	}

	public static string GetValueObjectName(IncrementalGeneratorRunStep step)
	{
		if (
			step.Outputs.Length > 0
			&& step.Outputs[0].Value is SourceGeneratorFramework.GeneratorResult<ScalarValueObjectModel> scalar
		)
		{
			return scalar.HasValue ? scalar.Value.TypeModel.Name : "(failed)";
		}

		if (
			step.Outputs.Length > 0
			&& step.Outputs[0].Value is SourceGeneratorFramework.GeneratorResult<ComplexValueObjectModel> complex
		)
		{
			return complex.HasValue ? complex.Value.TypeModel.Name : "(failed)";
		}

		// If the output is not a ValueObjectModel, it might be an AggregateInfo
		return "(unknown)";
	}
}
