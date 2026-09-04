using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Generators;

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
}
