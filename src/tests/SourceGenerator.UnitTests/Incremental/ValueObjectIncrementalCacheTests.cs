using System.Collections.Immutable;
using StepReason = Microsoft.CodeAnalysis.IncrementalStepRunReason;

namespace Purview.EventSourcing.SourceGenerator.Incremental;

/// <summary>
/// Stage-by-stage caching for the value-object pipeline, driven by the framework's
/// <c>GenerateIncrementalAsync</c> runner over a single shared driver and compilation.
/// </summary>
public sealed class ValueObjectIncrementalCacheTests : ValueObjectSourceGeneratorTestBase
{
	const string Source = """
		namespace Testing
		{
			[Purview.EventSourcing.Serialization.Scalar]
			public readonly partial record struct EmailAddress
			{
				public string Value { get; }
			}
		}
		""";

	const string ChangedSource = """
		namespace Testing
		{
			[Purview.EventSourcing.Serialization.Scalar(GenerateEmpty = false)]
			public readonly partial record struct EmailAddress
			{
				public string Value { get; }
			}
		}
		""";

	static ImmutableDictionary<string, ImmutableArray<StepReason>> StepReasons(IncrementalCacheRun run)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<StepReason>>();
		foreach (var pair in run.Steps)
			builder[pair.Key] =
			[
				.. pair.Value.SelectMany(static step => step.Outputs.Select(static output => output.Reason)),
			];
		return builder.ToImmutable();
	}

	[Test]
	public async Task IdenticalRerun_AllStagesCached(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync([Source], cancellationToken: cancellationToken);

		var second = StepReasons(result.Runs[1]);
		string[] frameworkStages = ["GetGenerationConfiguration", "GetGenerationContext_EmptyCapabilities"];
		await Assert
			.That(
				frameworkStages.All(stage =>
					second.TryGetValue(stage, out var reasons)
					&& reasons.All(r => r is StepReason.Cached or StepReason.Unchanged)
				)
			)
			.IsTrue();
	}

	[Test]
	public async Task SourceChange_MarksTargetStageModified_PropertyStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateIncrementalAsync(
			[new IncrementalRunInput([Source]), new IncrementalRunInput([ChangedSource])],
			cancellationToken: cancellationToken
		);

		var second = StepReasons(result.Runs[1]);
		await Assert.That(second["GetScalarValueObjectTargets"]).Contains(StepReason.Modified);
		// The generation configuration depends only on analyzer config options.
		await Assert.That(second["GetGenerationConfiguration"].All(r => r == StepReason.Cached)).IsTrue();
	}

	[Test]
	public async Task PropertyChange_MarksPropertyStagesModified_TargetStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateIncrementalAsync(
			[
				new IncrementalRunInput([Source]),
				new IncrementalRunInput([Source], [("build_property.DisableEventSourcingSourceGenerator", "true")]),
			],
			cancellationToken: cancellationToken
		);

		var second = StepReasons(result.Runs[1]);
		await Assert.That(second["GetGenerationConfiguration"]).Contains(StepReason.Modified);
		await Assert.That(second["GetScalarValueObjectTargets"]).DoesNotContain(StepReason.Modified);
	}
}
