using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Generators;
using StepReason = Microsoft.CodeAnalysis.IncrementalStepRunReason;

namespace Purview.EventSourcing.SourceGenerator.Incremental;

public sealed class AggregateSourceGeneratorIncrementalTests
{
	const string OrderAggregateSource = """
		using Purview.EventSourcing.Aggregates;

		namespace Testing
		{
			[Aggregate]
			public partial class OrderAggregate : AggregateBase
			{
				public string CustomerId { get; private set; }

				[Event]
				public partial void CreateOrder(string customerId);
			}
		}
		""";

	const string ModifiedOrderAggregateSource = """
		using Purview.EventSourcing.Aggregates;

		namespace Testing
		{
			[Aggregate]
			public partial class OrderAggregate : AggregateBase
			{
				public string CustomerId { get; private set; }
				public decimal Total { get; private set; }

				[Event]
				public partial void CreateOrder(string customerId);

				[Event]
				public partial void UpdateTotal(decimal total);
			}
		}
		""";

	const string CustomerAggregateSource = """
		using Purview.EventSourcing.Aggregates;

		namespace Testing
		{
			[Aggregate]
			public partial class CustomerAggregate : AggregateBase
			{
				public string Name { get; private set; }

				[Event]
				public partial void RegisterCustomer(string name);
			}
		}
		""";

	[Test]
	public async Task Generate_FirstRun_AllAggregateTargetsNew(CancellationToken cancellationToken)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<AggregateSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(driver.GetRunResult().Results[0], "GetAggregateTargets");

		await Assert.That(steps.Length).IsEqualTo(2);
		await Assert
			.That(steps.All(step => IncrementalGeneratorTestHarness.GetReason(step) == IncrementalStepRunReason.New))
			.IsTrue();
	}

	[Test]
	public async Task Generate_RerunWithUnchangedCompilation_AggregateTargetsCached(CancellationToken cancellationToken)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<AggregateSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(driver.GetRunResult().Results[0], "GetAggregateTargets");

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
	public async Task Generate_GivenChangeToOneAggregate_OnlyThatTargetModified(CancellationToken cancellationToken)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<AggregateSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var modifiedCompilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(ModifiedOrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
		]);
		driver = driver.RunGeneratorsAndUpdateCompilation(modifiedCompilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(driver.GetRunResult().Results[0], "GetAggregateTargets");

		var orderStep = steps.Single(step =>
			IncrementalGeneratorTestHarness.GetAggregateName(step) == "OrderAggregate"
		);
		var customerStep = steps.Single(step =>
			IncrementalGeneratorTestHarness.GetAggregateName(step) == "CustomerAggregate"
		);

		await Assert
			.That(IncrementalGeneratorTestHarness.GetReason(orderStep))
			.IsEqualTo(IncrementalStepRunReason.Modified);
		await Assert
			.That(IncrementalGeneratorTestHarness.GetReason(customerStep))
			.IsNotEqualTo(IncrementalStepRunReason.Modified);
	}

	[Test]
	public async Task Generate_GivenAggregateDeleted_TargetRemoved(CancellationToken cancellationToken)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<AggregateSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var reducedCompilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
		]);
		driver = driver.RunGeneratorsAndUpdateCompilation(reducedCompilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(driver.GetRunResult().Results[0], "GetAggregateTargets");

		await Assert.That(steps.Length).IsEqualTo(2);
		await Assert
			.That(
				steps.Count(step => IncrementalGeneratorTestHarness.GetReason(step) == IncrementalStepRunReason.Removed)
			)
			.IsEqualTo(1);

		var orderStep = steps.Single(step =>
			IncrementalGeneratorTestHarness.GetReason(step) != IncrementalStepRunReason.Removed
			&& IncrementalGeneratorTestHarness.GetAggregateName(step) == "OrderAggregate"
		);
		await Assert
			.That(IncrementalGeneratorTestHarness.GetReason(orderStep))
			.IsNotEqualTo(IncrementalStepRunReason.Modified);
	}

	[Test]
	public async Task Generate_GivenAggregateAdded_OnlyNewTargetAdded(CancellationToken cancellationToken)
	{
		const string additionalAggregateSource = """
			using Purview.EventSourcing.Aggregates;

			namespace Testing
			{
				[Aggregate]
				public partial class InventoryAggregate : AggregateBase
				{
					public int Quantity { get; private set; }

					[Event]
					public partial void AdjustQuantity(int quantity);
				}
			}
			""";

		var driver = IncrementalGeneratorTestHarness.CreateDriver<AggregateSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var expandedCompilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
			IncrementalGeneratorTestHarness.ParseTree(additionalAggregateSource, "Inventory.cs"),
		]);
		driver = driver.RunGeneratorsAndUpdateCompilation(expandedCompilation, out _, out _, cancellationToken);

		var steps = IncrementalGeneratorTestHarness.GetSteps(driver.GetRunResult().Results[0], "GetAggregateTargets");

		await Assert.That(steps.Length).IsEqualTo(3);
		await Assert
			.That(steps.Count(step => IncrementalGeneratorTestHarness.GetReason(step) == IncrementalStepRunReason.New))
			.IsEqualTo(1);

		var inventoryStep = steps.Single(step =>
			IncrementalGeneratorTestHarness.GetAggregateName(step) == "InventoryAggregate"
		);
		await Assert
			.That(IncrementalGeneratorTestHarness.GetReason(inventoryStep))
			.IsEqualTo(IncrementalStepRunReason.New);

		var orderStep = steps.Single(step =>
			IncrementalGeneratorTestHarness.GetAggregateName(step) == "OrderAggregate"
		);
		var customerStep = steps.Single(step =>
			IncrementalGeneratorTestHarness.GetAggregateName(step) == "CustomerAggregate"
		);
		await Assert
			.That(IncrementalGeneratorTestHarness.GetReason(orderStep))
			.IsNotEqualTo(IncrementalStepRunReason.Modified);
		await Assert
			.That(IncrementalGeneratorTestHarness.GetReason(customerStep))
			.IsNotEqualTo(IncrementalStepRunReason.Modified);
	}

	[Test]
	public async Task Generate_GivenChangeToOneAggregate_OtherAggregateOutputUnchanged(
		CancellationToken cancellationToken
	)
	{
		var driver = IncrementalGeneratorTestHarness.CreateDriver<AggregateSourceGenerator>();
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);
		var firstCustomerSource = GeneratedSourcesContaining(driver.GetRunResult().Results[0], "CustomerAggregate");

		var modifiedCompilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(ModifiedOrderAggregateSource, "Order.cs"),
			IncrementalGeneratorTestHarness.ParseTree(CustomerAggregateSource, "Customer.cs"),
		]);
		driver = driver.RunGeneratorsAndUpdateCompilation(modifiedCompilation, out _, out _, cancellationToken);

		var secondCustomerSource = GeneratedSourcesContaining(driver.GetRunResult().Results[0], "CustomerAggregate");
		await Assert.That(secondCustomerSource).IsEqualTo(firstCustomerSource);
	}

	[Test]
	public async Task Generate_GivenInvalidPartialCode_ThenRecovery_RegeneratesAndCaches(
		CancellationToken cancellationToken
	)
	{
		const string nonPartialAggregateSource = """
			using Purview.EventSourcing.Aggregates;

			namespace Testing
			{
				[Aggregate]
				public class OrderAggregate : AggregateBase
				{
					public string CustomerId { get; private set; }

					[Event]
					public partial void CreateOrder(string customerId);
				}
			}
			""";

		var driver = IncrementalGeneratorTestHarness.CreateDriver<AggregateSourceGenerator>();
		var invalidCompilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(nonPartialAggregateSource, "Order.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(invalidCompilation, out _, out _, cancellationToken);
		await Assert.That(AggregateSources(driver.GetRunResult().Results[0])).IsEmpty();

		driver = driver.RunGeneratorsAndUpdateCompilation(invalidCompilation, out _, out _, cancellationToken);
		var invalidRerunSteps = IncrementalGeneratorTestHarness.GetSteps(
			driver.GetRunResult().Results[0],
			"GetAggregateTargets"
		);
		await Assert
			.That(
				invalidRerunSteps.All(step =>
				{
					var reason = IncrementalGeneratorTestHarness.GetReason(step);
					return reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged;
				})
			)
			.IsTrue();

		var validCompilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
		]);
		driver = driver.RunGeneratorsAndUpdateCompilation(validCompilation, out _, out _, cancellationToken);

		await Assert.That(AggregateSources(driver.GetRunResult().Results[0])).IsNotEmpty();
	}

	[Test]
	public async Task Generate_GivenUnrelatedAdditionalFileChange_TargetsUnaffected(CancellationToken cancellationToken)
	{
		var unrelated = new InMemoryAdditionalText("notes.txt", "first");
		var driver = IncrementalGeneratorTestHarness.CreateDriver<AggregateSourceGenerator>(
			ImmutableArray.Create<AdditionalText>(unrelated)
		);
		var compilation = IncrementalGeneratorTestHarness.CreateCompilation([
			IncrementalGeneratorTestHarness.ParseTree(OrderAggregateSource, "Order.cs"),
		]);

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);
		var firstGenerated = GeneratedSourceTexts(driver.GetRunResult().Results[0]);

		driver = driver.ReplaceAdditionalText(unrelated, new InMemoryAdditionalText("notes.txt", "second"));
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		var result = driver.GetRunResult().Results[0];
		await Assert.That(StepReasons(result, "GetAggregateTargets")).DoesNotContain(StepReason.Modified);
		await Assert.That(StepReasons(result, "EventContractManifest")).DoesNotContain(StepReason.Modified);

		var secondGenerated = GeneratedSourceTexts(result);
		await Assert.That(secondGenerated).IsEquivalentTo(firstGenerated);
	}

	static ImmutableArray<GeneratedSourceResult> AggregateSources(GeneratorRunResult result) =>
		[
			.. result.GeneratedSources.Where(static source =>
				!EventSourcingGeneratorTestOptions.AggregateGeneratedAttributes.Contains(
					source.HintName,
					StringComparer.Ordinal
				)
			),
		];

	static ImmutableArray<string> GeneratedSourceTexts(GeneratorRunResult result) =>
		[
			.. result
				.GeneratedSources.Select(static source => source.SourceText.ToString())
				.OrderBy(static source => source, StringComparer.Ordinal),
		];

	static string GeneratedSourcesContaining(GeneratorRunResult result, string fragment) =>
		result
			.GeneratedSources.Select(static source => source.SourceText.ToString())
			.Single(source => source.Contains(fragment, StringComparison.Ordinal));

	static ImmutableArray<StepReason> StepReasons(GeneratorRunResult result, string stepName) =>
		[
			.. IncrementalGeneratorTestHarness
				.GetSteps(result, stepName)
				.SelectMany(static step => step.Outputs.Select(static output => output.Reason)),
		];
}
