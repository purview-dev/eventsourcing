using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Generators;

namespace Purview.EventSourcing.SourceGenerator.Incremental;

public sealed class ValueObjectSourceGeneratorIncrementalTests
{
	const string EmailScalarSource = """
		using Purview.EventSourcing.Serialization;

		namespace Testing
		{
			[Scalar]
			public readonly partial record struct EmailAddress
			{
				public string Value { get; }
			}
		}
		""";

	const string ModifiedEmailScalarSource = """
		using Purview.EventSourcing.Serialization;

		namespace Testing
		{
			[Scalar(GenerateEmpty = false)]
			public readonly partial record struct EmailAddress
			{
				public string Value { get; }
			}
		}
		""";

	const string MoneyScalarSource = """
		using Purview.EventSourcing.Serialization;

		namespace Testing
		{
			[Scalar("Amount")]
			public readonly partial record struct Money
			{
				public decimal Amount { get; }
			}
		}
		""";

	[Test]
	public async Task Generate_FirstRun_AllScalarTargetsNew(CancellationToken cancellationToken)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<ValueObjectSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(EmailScalarSource, "Email.cs"),
			IncrementalGeneratorTestHarness.ParseTree(MoneyScalarSource, "Money.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(
			driver.GetRunResult().Results[0],
			"GetScalarValueObjectTargets"
		);

		await Assert.That(steps.Length).IsEqualTo(2);
		await Assert
			.That(steps.All(step => IncrementalGeneratorTestHarness.GetReason(step) == IncrementalStepRunReason.New))
			.IsTrue();
	}

	[Test]
	public async Task Generate_RerunWithUnchangedCompilation_ScalarTargetsCached(CancellationToken cancellationToken)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<ValueObjectSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(EmailScalarSource, "Email.cs"),
			IncrementalGeneratorTestHarness.ParseTree(MoneyScalarSource, "Money.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(
			driver.GetRunResult().Results[0],
			"GetScalarValueObjectTargets"
		);

		await Assert.That(steps.Length).IsEqualTo(2);
		await Assert
			.That(
				steps.All(step =>
				{
					var reason = IncrementalGeneratorTestHarness.GetReason(step);
					return reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged;
				})
			)
			.IsTrue();
	}

	[Test]
	public async Task Generate_GivenChangeToOneScalar_OnlyThatTargetModified(CancellationToken cancellationToken)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<ValueObjectSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(EmailScalarSource, "Email.cs"),
			IncrementalGeneratorTestHarness.ParseTree(MoneyScalarSource, "Money.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var modifiedCompilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(ModifiedEmailScalarSource, "Email.cs"),
			IncrementalGeneratorTestHarness.ParseTree(MoneyScalarSource, "Money.cs"),
		]);
		driver = driver.RunGeneratorsAndUpdateCompilation(modifiedCompilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(
			driver.GetRunResult().Results[0],
			"GetScalarValueObjectTargets"
		);

		await Assert.That(steps.Length).IsEqualTo(2);
		await Assert
			.That(
				steps.Count(step =>
					IncrementalGeneratorTestHarness.GetReason(step) == IncrementalStepRunReason.Modified
				)
			)
			.IsEqualTo(1);
		await Assert
			.That(steps.Count(step => IncrementalGeneratorTestHarness.GetReason(step) == IncrementalStepRunReason.New))
			.IsEqualTo(0);
	}

	[Test]
	public async Task Generate_GivenScalarDeleted_TargetRemoved(CancellationToken cancellationToken)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<ValueObjectSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(EmailScalarSource, "Email.cs"),
			IncrementalGeneratorTestHarness.ParseTree(MoneyScalarSource, "Money.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var reducedCompilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(EmailScalarSource, "Email.cs"),
		]);
		driver = driver.RunGeneratorsAndUpdateCompilation(reducedCompilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(
			driver.GetRunResult().Results[0],
			"GetScalarValueObjectTargets"
		);

		await Assert.That(steps.Length).IsEqualTo(2);
		await Assert
			.That(
				steps.Count(step => IncrementalGeneratorTestHarness.GetReason(step) == IncrementalStepRunReason.Removed)
			)
			.IsEqualTo(1);
		await Assert
			.That(
				steps.Count(step =>
					IncrementalGeneratorTestHarness.GetReason(step) == IncrementalStepRunReason.Modified
				)
			)
			.IsEqualTo(0);
	}
}
