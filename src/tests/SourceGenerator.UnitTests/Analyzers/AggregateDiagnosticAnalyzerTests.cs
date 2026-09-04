using Purview.SourceGeneratorFramework;

namespace Purview.EventSourcing.SourceGenerator.Analyzers;

public sealed class AggregateDiagnosticAnalyzerTests : AnalyzerTestBase<AggregateDiagnosticAnalyzer>
{
	[Test]
	public async Task Generate_GivenNonPartialAggregate_ReportsAggregateMustBePartial(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public class OrderAggregate : AggregateBase
				{
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AggregateMustBePartial);
	}

	[Test]
	public async Task Generate_GivenAggregateWithoutAggregateBase_ReportsAggregateMustInheritAggregateBase(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				public class SomeBase
				{
				}

				[Aggregate]
				public partial class OrderAggregate : SomeBase
				{
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AggregateMustInheritAggregateBase);
	}

	[Test]
	public async Task Generate_GivenNestedAggregate_ReportsNestedAggregatesAreNotSupported(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				public class Outer
				{
					[Aggregate]
					public partial class InnerAggregate : AggregateBase
					{
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.NestedAggregatesAreNotSupported);
	}

	[Test]
	public async Task Generate_GivenGenericAggregate_ReportsGenericAggregatesAreNotSupported(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public partial class OrderAggregate<T> : AggregateBase
				{
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.GenericAggregatesAreNotSupported);
	}

	[Test]
	public async Task Generate_GivenManualRegisterEvents_ReportsManualRegisterEventsIsNotSupported(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public partial class OrderAggregate : AggregateBase
				{
					protected override void RegisterEvents()
					{
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ManualRegisterEventsIsNotSupported);
	}

	[Test]
	public async Task Generate_GivenNonPrivateSetter_ReportsAggregatePropertySetterShouldBePrivate(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public partial class OrderAggregate : AggregateBase
				{
					public string CustomerId { get; set; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AggregatePropertySetterShouldBePrivate);
	}

	[Test]
	public async Task Generate_GivenPlainCollectionProperty_ReportsEventStoreCollectionRequirement(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public partial class OrderAggregate : AggregateBase
				{
					public global::System.Collections.Generic.List<string> Items { get; private set; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(DiagnosticLibrary.AggregatePropertyCollectionTypeMustUseEventStoreCollections);
	}

	[Test]
	public async Task Generate_GivenNonPartialEventMethod_ReportsEventMethodMustBePartial(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public partial class OrderAggregate : AggregateBase
				{
					public string CustomerId { get; private set; }

					[Event]
					public void CreateOrder(string customerId)
					{
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.EventMethodMustBePartial);
	}

	[Test]
	public async Task Generate_GivenDuplicateGeneratedEventName_ReportsDuplicateGeneratedEventName(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public partial class OrderAggregate : AggregateBase
				{
					public string Value { get; private set; }
					public int Count { get; private set; }

					[Event]
					public partial void Update(string value);

					[Event]
					public partial void Update(int count);
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.DuplicateGeneratedEventName);
	}

	[Test]
	public async Task Generate_GivenDuplicateSchemaVersion_ReportsDuplicateEventSchemaVersionOnAggregate(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public partial class OrderAggregate : AggregateBase
				{
					public string Name { get; private set; }

					[Event(Version = 2)]
					public partial void Rename(string name);

					[Event(Version = 2)]
					public partial void UpdateName(string name);
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate);
	}

	[Test]
	public async Task Generate_GivenEventParameterWithoutMatchingProperty_ReportsEventParameterMustMapToWritableProperty(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Aggregate]
				public partial class OrderAggregate : AggregateBase
				{
					[Event]
					public partial void CreateOrder(string customerId);
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.EventParameterMustMapToWritableProperty);
	}

	[Test]
	public async Task Generate_GivenValidAggregate_ReportsNoDiagnostics(CancellationToken cancellationToken)
	{
		const string source = """
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

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	protected override AnalyzerTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		AnalyzerTestOptions options,
		CancellationToken cancellationToken
	)
	{
		return base.OnBeforeRun(
			sources,
			options
				// The aggregate attributes are emitted by the source generator, so they must be
				// present in the compilation for the analyzer to match on them.
				.WithAdditionalSources(AggregateAttributeEmitter.Emit().Select(m => m.Source))
				.WithAdditionalNamespaces(TypeLibrary.AggregateNamespace),
			cancellationToken
		);
	}
}
