using Purview.EventSourcing.EntityFrameworkCore.SourceGenerator.Heleprs;

namespace Purview.EventSourcing.EntityFrameworkCore.SourceGenerator;

public sealed class EFSnapshotShapeAnalyzerTests
	: TUnitSourceGeneratorTestBase<EFSourceGenerator, EFSourceGeneratorTestOptions>
{
	[Test]
	public async Task Analyze_GivenDictionaryInAggregateGraph_RecommendsOpaqueOrEntryCollection(
		CancellationToken cancellationToken
	)
	{
		const string source = """
namespace Testing;

[Aggregate]
sealed class ReportAggregate
{
	public AssetDetails Details { get; } = new();
}

sealed class AssetDetails
{
	public IReadOnlyDictionary<string, int> OperatingSystemDistribution { get; } = new Dictionary<string, int>();
}
""";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.UnsupportedDictionary);
	}

	[Test]
	public async Task Analyze_GivenOpaqueDictionary_AllowsSnapshotShape(CancellationToken cancellationToken)
	{
		const string source = """
namespace Testing;

[Aggregate]
sealed class ReportAggregate
{
	public AssetDetails Details { get; } = new();
}

sealed class AssetDetails
{
	[EFOpaque]
	public IReadOnlyDictionary<string, int> Values { get; } = new Dictionary<string, int>();
}
""";
		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.UnsupportedDictionary);
	}

	[Test]
	public async Task Analyze_GivenOpaquePropertyInSnapshotQuery_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
namespace Testing;

[Aggregate]
sealed class ReportAggregate
{
	[EFOpaque]
	public IReadOnlyDictionary<string, int> Values { get; } = new Dictionary<string, int>();
}

static class Store
{
	public static void QueryAsync(System.Func<ReportAggregate, bool> predicate) { }
}

static class Consumer
{
	public static void Query() => Store.QueryAsync(report => report.Values.Count > 0);
}
""";
		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.OpaqueQuery);
	}

	protected override EFSourceGeneratorTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		EFSourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	) =>
		base.OnBeforeRun(
			sources,
			options with
			{
				DefaultNamespaces = options.DefaultNamespaces.Add(TypeLibrary.EFOpaqueAttribute.Namespace!),
				AnalyzerTypes = [typeof(EFSnapshotShapeAnalyzer)],
			},
			cancellationToken
		);
}
