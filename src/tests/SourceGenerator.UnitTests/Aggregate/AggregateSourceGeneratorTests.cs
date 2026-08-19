using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Generators;

namespace Purview.EventSourcing.SourceGenerator.Aggregate;

public sealed class AggregateSourceGeneratorTests
	: EventSourcingSourceGeneratorTestBase<AggregateSourceGenerator>
{
	[Test]
	public async Task Generate_GivenEmptySource_GeneratesAttributesOnly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	public class Empty { }
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert
			.That(result.AllSyntaxTrees.Length)
			.IsEqualTo(EventSourcingGeneratorTestOptions.ExpectedFileCount);
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_GeneratesExpectedCode(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId, decimal total);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void UpdateTotal(decimal total);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert that 4 attribute files + 1 generated aggregate file
		await Assert
			.That(result.AllSyntaxTrees.Length)
			.IsEqualTo(EventSourcingGeneratorTestOptions.ExpectedFileCountPlusGen);
	}

	[Test]
	public async Task Generate_GivenAggregateWithNoEvents_GeneratesEmptyRegisterEvents(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class EmptyAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert
			.That(result.AllSyntaxTrees.Length)
			.IsEqualTo(EventSourcingGeneratorTestOptions.ExpectedFileCountPlusGen);
	}

	[Test]
	public async Task Generate_GivenAggregateWithParameterlessEvent_GeneratesCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class CounterAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public int Count { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Increment();
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert
			.That(result.AllSyntaxTrees.Length)
			.IsEqualTo(EventSourcingGeneratorTestOptions.ExpectedFileCountPlusGen);
	}

	[Test]
	public async Task Generate_GivenNonPartialClass_DoesNotGenerate(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public class NonPartialAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		protected override void RegisterEvents() { }
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert that only attribute files, no generated aggregate
		await Assert
			.That(result.AllSyntaxTrees.Length)
			.IsEqualTo(EventSourcingGeneratorTestOptions.ExpectedFileCount);
		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.AggregateMustBePartial);
	}

	[Test]
	public async Task Generate_GivenMultipleParameters_GeneratesAllProperties(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ProductAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; }
		public decimal Price { get; private set; }
		public int Quantity { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetProduct(string name, decimal price, int quantity);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert
			.That(result.AllSyntaxTrees.Length)
			.IsEqualTo(EventSourcingGeneratorTestOptions.ExpectedFileCountPlusGen);
	}

	[Test]
	public async Task Generate_GivenComputedParameterIsExplicitlyPassed_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public enum ReportProcessingStatus
	{
		Uploaded,
		Complete,
		Failed
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportUploadAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Blob { get; private set; }
		public object Summary { get; private set; }
		public ReportProcessingStatus Status { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial ReportUploadAggregate MarkAsCompleted(
			string blob,
			object summary,
			[Purview.EventSourcing.Aggregates.Computed] ReportProcessingStatus status = default
		);
	}

	public static class Caller
	{
		public static void Run(ReportUploadAggregate aggregate)
		{
			aggregate.MarkAsCompleted(""blob://1"", new object(), ReportProcessingStatus.Failed);
		}
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(DiagnosticLibrary.ComputedParameterCannotBeSetByCaller);
	}

	[Test]
	public async Task Generate_GivenComputedParameterWithNoComputeHook_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public enum ReportProcessingStatus
	{
		Uploaded,
		Complete,
		Failed
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportUploadAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Blob { get; private set; }
		public object Summary { get; private set; }
		public ReportProcessingStatus Status { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(EventName = ""CompletedEvent"")]
		public partial ReportUploadAggregate MarkAsCompleted(
			string blob,
			object summary,
			[Purview.EventSourcing.Aggregates.Computed] ReportProcessingStatus status = default
		);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(
				DiagnosticLibrary.AggregatePropertyCollectionTypeMustUseEventStoreCollections
			);
		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(
				DiagnosticLibrary.NullableScalarEqualityNullComparisonShouldUsePatternMatching
			);
	}

	[Test]
	public async Task Generate_GivenNullableScalarComparedToNullWithEquality_ReportsPatternMatchingWarning(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Serialization.Scalar]
	public readonly partial record struct ProjectId
	{
		public string Value { get; }
		private ProjectId(string value) => Value = value;
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetName(string name);

		public bool ShouldClear(ProjectId? projectId) => projectId == null;
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(
				DiagnosticLibrary.NullableScalarEqualityNullComparisonShouldUsePatternMatching
			);
	}

	[Test]
	public async Task Generate_GivenNullableScalarComparedToNullWithPatternMatching_DoesNotReportWarning(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Serialization.Scalar]
	public readonly partial record struct ProjectId
	{
		public string Value { get; }
		private ProjectId(string value) => Value = value;
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetName(string name);

		public bool ShouldClear(ProjectId? projectId) => projectId is null;
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(
				DiagnosticLibrary.NullableScalarEqualityNullComparisonShouldUsePatternMatching
			);
	}

	[Test]
	public async Task Generate_GivenScalarWrappingComplexValue_ReportsQueryTranslationWarning(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public sealed class ReportSummary
	{
		public int FailedLines { get; set; }
	}

	[Purview.EventSourcing.Serialization.Scalar]
	public readonly partial record struct ReportSummaryScalar
	{
		public ReportSummary Value { get; }
		private ReportSummaryScalar(ReportSummary value) => Value = value;
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public ReportSummaryScalar Summary { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetSummary(ReportSummary value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		await Assert
			.That(result)
			.HasDiagnostic(DiagnosticLibrary.ScalarComplexValueMayNotTranslateInSqlSnapshots);
	}

	[Test]
	public async Task Generate_GivenScalarWrappingPrimitiveValue_DoesNotReportQueryTranslationWarning(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Serialization.Scalar]
	public readonly partial record struct ProjectId
	{
		public string Value { get; }
		private ProjectId(string value) => Value = value;
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public ProjectId ProjectId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetProjectId(string projectId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(
				DiagnosticLibrary.ScalarComplexValueMayNotTranslateInSqlSnapshots
			);
	}

	[Test]
	public async Task Generate_GivenComputedParameterWithOnlyOnComputingHook_DoesNotReportHookDiagnostics(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public enum ReportProcessingStatus
	{
		Uploaded,
		Complete,
		Failed
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportUploadAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Blob { get; private set; }
		public object Summary { get; private set; }
		public ReportProcessingStatus Status { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(EventName = ""CompletedEvent"")]
		public partial ReportUploadAggregate MarkAsCompleted(
			string blob,
			object summary,
			[Purview.EventSourcing.Aggregates.Computed] ReportProcessingStatus status = default
		);

		partial void OnComputingCompletedEvent(ref ReportProcessingStatus status) => status = ReportProcessingStatus.Complete;
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).DoesNotHaveDiagnostic("EVENTSTORE018");
		await Assert.That(result).DoesNotHaveDiagnostic("EVENTSTORE019");

		await Assert.That(result.EnsureValid).ThrowsNothing();
	}

	[Test]
	public async Task Generate_GivenComputedParameterWithInvalidOnComputingSignature_ReportsMissingHookDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public enum ReportProcessingStatus
	{
		Uploaded,
		Complete,
		Failed
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportUploadAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Blob { get; private set; }
		public object Summary { get; private set; }
		public ReportProcessingStatus Status { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(EventName = ""CompletedEvent"")]
		public partial ReportUploadAggregate MarkAsCompleted(
			string blob,
			object summary,
			[Purview.EventSourcing.Aggregates.Computed] ReportProcessingStatus status = default
		);

		// Invalid: missing ref
		partial void OnComputingCompletedEvent(ReportProcessingStatus status) { }
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(
				DiagnosticLibrary.AggregatePropertyCollectionTypeMustUseEventStoreCollections
			);
		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(
				DiagnosticLibrary.NullableScalarEqualityNullComparisonShouldUsePatternMatching
			);
	}

	[Test]
	public async Task Generate_GivenComputedParameter_GeneratedSourceContainsComputingAndRaisingHookOverloads(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public enum ReportProcessingStatus
	{
		Uploaded,
		Complete,
		Failed
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportUploadAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Blob { get; private set; }
		public object Summary { get; private set; }
		public ReportProcessingStatus Status { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(EventName = ""CompletedEvent"")]
		public partial ReportUploadAggregate MarkAsCompleted(
			string blob,
			object summary,
			[Purview.EventSourcing.Aggregates.Computed] ReportProcessingStatus status = default
		);

		partial void OnComputingCompletedEvent(ref ReportProcessingStatus status) => status = ReportProcessingStatus.Complete;
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(generatedSource)
			.Contains(
				"partial void OnComputingCompletedEvent(ref global::Testing.ReportProcessingStatus status);"
			);
		await Assert
			.That(generatedSource)
			.Contains(
				"partial void OnComputingCompletedEvent(ref string blob, ref object summary, ref global::Testing.ReportProcessingStatus status);"
			);
		await Assert
			.That(generatedSource)
			.Contains("partial void OnRaisingCompletedEvent(ref string blob, ref object summary);");
		await Assert
			.That(generatedSource)
			.Contains(
				"partial void OnRaisingCompletedEvent(ref string blob, ref object summary, ref global::Testing.ReportProcessingStatus status);"
			);
	}

	[Test]
	public async Task Generate_GivenComputedParameterWithOnlyOnComputingWithoutComputedValuesHook_DoesNotReportHookDiagnostics(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public enum ReportProcessingStatus
	{
		Uploaded,
		Complete,
		Failed
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportUploadAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Blob { get; private set; }
		public object Summary { get; private set; }
		public ReportProcessingStatus Status { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(EventName = ""CompletedEvent"")]
		public partial ReportUploadAggregate MarkAsCompleted(
			string blob,
			object summary,
			[Purview.EventSourcing.Aggregates.Computed] ReportProcessingStatus status = default
		);

		partial void OnComputingCompletedEvent(ref string blob, ref object summary)
		{
		}
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).DoesNotHaveDiagnostic("EVENTSTORE018");
		await Assert.That(result).DoesNotHaveDiagnostic("EVENTSTORE019");
	}

	[Test]
	public async Task Generate_GivenEventNameOverride_HookNamesUseOverriddenEventName(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public enum ReportProcessingStatus
	{
		None,
		Complete
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ReportUploadAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Blob { get; private set; }
		public object Summary { get; private set; }
		public ReportProcessingStatus Status { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(EventName = ""MarkAsCompleted"")]
		public partial ReportUploadAggregate MarkAsComplete(
			string blob,
			object summary,
			[Purview.EventSourcing.Aggregates.Computed] ReportProcessingStatus status = default
		);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(generatedSource).Contains("OnComputingMarkAsCompletedEvent");
		await Assert.That(generatedSource).Contains("OnRaisingMarkAsCompletedEvent");
		await Assert.That(generatedSource).DoesNotContain("OnComputingMarkAsCompleted2Event");
	}

	[Test]
	public async Task Generate_ProducesNoDiagnosticErrors(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class SimpleAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(string value);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert that no generator exceptions
		foreach (var genResult in result.DriverResult.Results)
		{
			await Assert.That(genResult.Exception).IsNull();
		}

		// Assert that no errors in the output compilation (excluding pre-existing diagnostic warnings)
		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_GeneratedSourceContainsEventClass(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that event class uses the default namespace pattern
		await Assert.That(generatedSource).Contains("namespace Testing.Order");
		await Assert.That(generatedSource).Contains("public sealed class OrderCreated");
		await Assert
			.That(generatedSource)
			.Contains(": global::Purview.EventSourcing.Aggregates.Events.EventBase");
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_GeneratedSourceContainsJsonConverterSupport(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(generatedSource)
			.Contains("JsonConverter(typeof(OrderAggregateJsonConverter))");
		await Assert
			.That(generatedSource)
			.Contains(
				"internal static OrderAggregate CreateFromJsonModel(OrderAggregateJsonModel jsonModel)"
			);
		await Assert.That(generatedSource).Contains("sealed class OrderAggregateJsonConverter");
		await Assert.That(generatedSource).Contains("sealed class OrderAggregateJsonModel");
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_GeneratedSourceContainsEventProperties(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId, decimal total);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that event properties are generated with PascalCase names
		await Assert.That(generatedSource).Contains("public string CustomerId { get; set; }");
		await Assert.That(generatedSource).Contains("public decimal Total { get; set; }");
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_GeneratedSourceContainsBuildEventHash(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; }
		public int Count { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetOrder(string name, int count);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that BuildEventHash adds each property
		await Assert
			.That(generatedSource)
			.Contains("protected override void BuildEventHash(ref global::System.HashCode hash)");
		await Assert.That(generatedSource).Contains("hash.Add(Name);");
		await Assert.That(generatedSource).Contains("hash.Add(Count);");
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_GeneratedSourceContainsRegisterEvents(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void UpdateOrder(string customerId);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that RegisterEvents contains Register calls for each event
		await Assert.That(generatedSource).Contains("protected override void RegisterEvents()");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.OrderCreatedEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.OrderUpdatedEvent>(Apply);");
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_GeneratedSourceContainsApplyMethods(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that Apply method is generated with property assignments from event
		await Assert
			.That(generatedSource)
			.Contains("void Apply(global::Testing.OrderEvents.OrderCreatedEvent @event)");
		await Assert.That(generatedSource).Contains("CustomerId = @event.CustomerId;");
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_GeneratedSourceContainsCommandMethod(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId, decimal total);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that command method calls RecordAndApply with a new event
		await Assert
			.That(generatedSource)
			.Contains("public partial void CreateOrder(string customerId, decimal total)");
		await Assert
			.That(generatedSource)
			.Contains("var @event = new global::Testing.OrderEvents.OrderCreated");
		await Assert.That(generatedSource).Contains("RecordAndApply(@event);");
		await Assert.That(generatedSource).Contains("CustomerId = customerId,");
		await Assert.That(generatedSource).Contains("Total = total,");
	}

	[Test]
	public async Task Generate_GivenStringMappedEvent_GeneratedSourceContainsOrdinalGuard(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ProfileAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Rename(string name);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(generatedSource).Contains("public partial void Rename(string name)");
		await Assert
			.That(generatedSource)
			.Contains(
				"if (global::System.String.Equals(Name, name, global::System.StringComparison.Ordinal))"
			);
	}

	[Test]
	public async Task Generate_GivenMultiPropertyEvent_GeneratedSourceContainsCompoundEqualityGuard(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ProductAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; } = default!;
		public int Quantity { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Update(string name, int quantity);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(generatedSource)
			.Contains(
				"global::System.String.Equals(Name, name, global::System.StringComparison.Ordinal) && global::System.Collections.Generic.EqualityComparer<int>.Default.Equals(Quantity, quantity)"
			);
		await Assert.That(generatedSource).Contains("return;");
	}

	[Test]
	public async Task Generate_GivenAggregateReturningEventMethod_GeneratedSourceSupportsFluentChaining(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ProfileAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial ProfileAggregate Rename(string name);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(generatedSource)
			.Contains("public partial ProfileAggregate Rename(string name)");
		await Assert
			.That(generatedSource)
			.Contains(
				"if (global::System.String.Equals(Name, name, global::System.StringComparison.Ordinal))"
			);
		await Assert.That(generatedSource).Contains("return this;");
	}

	[Test]
	public async Task Generate_GivenBoolReturningEventMethod_GeneratedSourceReturnsFalseWhenUnchanged(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ProfileAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial bool Rename(string name);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(generatedSource).Contains("public partial bool Rename(string name)");
		await Assert
			.That(generatedSource)
			.Contains(
				"if (global::System.String.Equals(Name, name, global::System.StringComparison.Ordinal))"
			);
		await Assert.That(generatedSource).Contains("return false;");
		await Assert.That(generatedSource).Contains("return true;");
	}

	[Test]
	public async Task Generate_GivenParameterlessEvent_GeneratedSourceContainsEmptyEventAndRecordAndApplyWithNew(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class CounterAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public int Count { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Increment();
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();
		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		// Assert that parameterless event uses () constructor
		await Assert.That(generatedSource).Contains("public partial void Increment()");
		await Assert
			.That(generatedSource)
			.Contains(
				"private partial void Apply(global::Testing.CounterEvents.IncrementedEvent @event);"
			);
		await Assert
			.That(generatedSource)
			.Contains("protected override void BuildEventHash(ref global::System.HashCode _)");
		await Assert
			.That(generatedSource)
			.Contains("var @event = new global::Testing.CounterEvents.IncrementedEvent");
		await Assert.That(generatedSource).Contains("RecordAndApply(@event);");
		await Assert.That(errors.Select(static e => e.Id)).Contains("CS8795");
	}

	[Test]
	public async Task Generate_GivenImplicitlyConvertibleParameterType_GeneratedSourceUsesPropertyTypeForEqualityGuard(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public readonly record struct Name
	{
		public string Value { get; }

		private Name(string value) => Value = value;

		public static implicit operator Name(string value) => new(value);
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class CustomerAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public Name Name { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void ChangeName(string name);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();
		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		await Assert
			.That(generatedSource)
			.Contains(
				"global::System.Collections.Generic.EqualityComparer<global::Testing.Name>.Default.Equals(Name, __nameValue)"
			);
		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenPrivatePartialEventMethod_GeneratesPrivateMethodAndPublicEvent(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ToggleAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public bool IsActive { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		partial ToggleAggregate ChangeIsActive(bool isActive);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();
		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		await Assert
			.That(generatedSource)
			.Contains("private partial ToggleAggregate ChangeIsActive(bool isActive)");
		await Assert.That(generatedSource).Contains("public sealed class IsActiveChanged");
		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenAggregateWithNoEvents_GeneratedSourceContainsEmptyRegisterEvents(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class EmptyAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that RegisterEvents exists but has no Register calls
		await Assert.That(generatedSource).Contains("protected override void RegisterEvents()");
		// No Events namespace section should be generated
		await Assert.That(generatedSource).DoesNotContain("namespace Testing.EmptyEvents");
	}

	[Test]
	public async Task Generate_GivenMultipleEvents_GeneratedSourceContainsAllEventClasses(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId, decimal total);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void UpdateTotal(decimal total);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that both event classes exist
		await Assert.That(generatedSource).Contains("public sealed class OrderCreated");
		await Assert.That(generatedSource).Contains("public sealed class TotalUpdated");
	}

	[Test]
	public async Task Generate_GivenClassWithNoBaseClass_GeneratesAndAddsAggregateBaseToGeneratedPart(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class NotAnAggregate
	{
		[Purview.EventSourcing.Aggregates.Event]
		public partial void DoSomething(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(result.DriverResult.GeneratedTrees)
			.Count()
			.IsEqualTo(ExpectedFileCountPlusGen);
		await Assert
			.That(generatedSource)
			.Contains(
				"public partial class NotAnAggregate : global::Purview.EventSourcing.Aggregates.AggregateBase"
			);
		await Assert.That(result).DoesNotHaveDiagnostic("EVENTSTORE002");
	}

	[Test]
	public async Task Generate_GivenNonPartialMethod_MethodIsSkipped(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class MixedAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetName(string name);

		// This method is NOT partial, so it should be ignored even though it has the attribute
		[Purview.EventSourcing.Aggregates.Event]
		public void NonPartialMethod(string value) { }
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that only the partial method generates an event, the non-partial is skipped
		await Assert.That(generatedSource).Contains("NameSet");
		await Assert.That(generatedSource).DoesNotContain("NonPartialMethodEvent");

		await Assert.That(result).HasDiagnostic("EVENTSTORE007");
	}

	[Test]
	public async Task Generate_GivenClassWithOnlyInterfaces_GeneratesAndAddsAggregateBaseToGeneratedPart(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public interface ITaggable
	{
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class InterfaceOnlyAggregate : ITaggable
	{
		public string Value { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(generatedSource)
			.Contains(
				"public partial class InterfaceOnlyAggregate : global::Purview.EventSourcing.Aggregates.AggregateBase"
			);
		await Assert.That(result).DoesNotHaveDiagnostic("EVENTSTORE002");

		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(static d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();
		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenInternalAggregate_GeneratesInternalAccessModifier(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	internal partial class InternalAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(string value);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that the generated partial class uses 'internal' access modifier
		await Assert.That(generatedSource).Contains("internal partial class InternalAggregate");
	}

	[Test]
	public async Task Generate_GivenAttributeFiles_ContainsAggregateAttribute(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	public class Empty { }
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert that attribute files are generated
		var attributeSources = result
			.DriverResult.GeneratedTrees.Select(t => t.GetText().ToString())
			.ToList();

		await Assert.That(attributeSources).Count().IsEqualTo(ExpectedFileCount);

		var allAttributeSource = string.Join("\n", attributeSources);
		await Assert.That(allAttributeSource).Contains("class EmbeddedAttribute");
		await Assert.That(allAttributeSource).Contains("class PropertyAttribute");
		await Assert.That(allAttributeSource).Contains("class AggregateAttribute");
		await Assert.That(allAttributeSource).Contains("class AggregateDefaultsAttribute");
		await Assert.That(allAttributeSource).Contains("class EventAttribute");
		await Assert.That(allAttributeSource).Contains("class MetadataAttribute");
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_OutputCompilationHasNoErrors(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId, decimal total);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void UpdateTotal(decimal total);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert that no generator exceptions
		foreach (var genResult in result.DriverResult.Results)
		{
			await Assert.That(genResult.Exception).IsNull();
		}

		// Assert that no compilation errors
		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenGeneratedFile_HasAutoGeneratedHeader(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class SimpleAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		[Purview.EventSourcing.Aggregates.Event]
		public partial void DoWork();
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that generated file starts with auto-generated header
		await Assert.That(generatedSource).Contains("// <auto-generated />");
		await Assert.That(generatedSource).Contains("#nullable enable");
	}

	[Test]
	public async Task Generate_GivenEventWithDefaultVersion_GeneratesSchemaVersionOverrideOfOne(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that default version is 1
		await Assert.That(generatedSource).Contains("public override int SchemaVersion => 1;");
	}

	[Test]
	public async Task Generate_GivenEventWithExplicitVersion_GeneratesCorrectSchemaVersionOverride(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(Version = 3)]
		public partial void CreateOrder(string customerId);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that explicit version 3
		await Assert.That(generatedSource).Contains("public override int SchemaVersion => 3;");
	}

	[Test]
	public async Task Generate_GivenMultipleEventsWithDifferentVersions_GeneratesCorrectSchemaVersionForEach(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);

		[Purview.EventSourcing.Aggregates.Event(Version = 2)]
		public partial void UpdateTotal(decimal total);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that both SchemaVersion overrides appear
		var v1Index = generatedSource.IndexOf(
			"public override int SchemaVersion => 1;",
			StringComparison.Ordinal
		);
		var v2Index = generatedSource.IndexOf(
			"public override int SchemaVersion => 2;",
			StringComparison.Ordinal
		);

		await Assert.That(v1Index).IsGreaterThanOrEqualTo(0);
		await Assert.That(v2Index).IsGreaterThanOrEqualTo(0);
		// They should appear in event-declaration order (OrderCreated before TotalUpdated)
		await Assert.That(v1Index).IsLessThan(v2Index);
	}

	[Test]
	public async Task Generate_GivenEventWithNonPositiveVersion_ReportsVersionDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(Version = 0)]
		public partial void CreateOrder(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.EventSchemaVersionMustBePositive);
	}

	[Test]
	public async Task Generate_GivenMultipleEventsWithDuplicateVersions_ReportsDuplicateVersionDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(Version = 2)]
		public partial void CreateOrder(string customerId);

		[Purview.EventSourcing.Aggregates.Event(Version = 2)]
		public partial void UpdateTotal(decimal total);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate);
	}

	[Test]
	public async Task Generate_GivenExplicitVersionsWithGap_ReportsContiguousVersionDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(Version = 1)]
		public partial void CreateOrder(string customerId);

		[Purview.EventSourcing.Aggregates.Event(Version = 3)]
		public partial void UpdateTotal(decimal total);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(DiagnosticLibrary.EventSchemaVersionsShouldBeContiguous);
	}

	[Test]
	public async Task Generate_GivenContiguousExplicitVersions_DoesNotReportContiguousVersionDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event(Version = 2)]
		public partial void CreateOrder(string customerId);

		[Purview.EventSourcing.Aggregates.Event(Version = 3)]
		public partial void UpdateTotal(decimal total);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.EventSchemaVersionsShouldBeContiguous);
	}

	[Test]
	public async Task Generate_GivenVersionedEvent_GeneratedAttributeTemplateContainsVersionProperty(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	public class Empty { }
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert that the generated attribute file exposes a Version property
		var attributeSource = result.GetSource(TypeLibrary.Attributes.EventAttribute.TypeName);

		await Assert.That(attributeSource).Contains("int Version");
		await Assert.That(attributeSource).Contains("string? EventName");
		await Assert.That(attributeSource).Contains("string? EventNamespace");

		var aggregateAttributeSource = result.GetSource(
			TypeLibrary.Attributes.AggregateAttribute.TypeName
		);

		await Assert.That(aggregateAttributeSource).Contains("string? EventNamespace");
		await Assert.That(aggregateAttributeSource).Contains("string? EventSuffix");

		var aggregateDefaultsAttributeSource = result.GetSource(
			TypeLibrary.Attributes.AggregateDefaultsAttribute.TypeName
		);
		await Assert.That(aggregateDefaultsAttributeSource).Contains("string? EventSuffix");
	}

	[Test]
	public async Task Generate_GivenInferredEventName_AppliesSuffixByDefault(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(generatedSource).Contains("public sealed class OrderCreated");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.OrderCreatedEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("void Apply(global::Testing.OrderEvents.OrderCreatedEvent @event)");
	}

	[Test]
	public async Task Generate_GivenAssemblyEventSuffixOverride_UsesAssemblyConfiguredSuffix(
		CancellationToken cancellationToken
	)
	{
		var source =
			@"[assembly: Purview.EventSourcing.Aggregates.AggregateDefaults(EventSuffix = ""DomainEvent"")]

namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(generatedSource).Contains("public sealed class OrderCreatedDomainEvent");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.OrderCreatedDomainEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("void Apply(global::Testing.OrderEvents.OrderCreatedDomainEvent @event)");
	}

	[Test]
	public async Task Generate_GivenAggregateEventSuffixOverride_PrefersAggregateSuffixOverAssembly(
		CancellationToken cancellationToken
	)
	{
		var source =
			@"[assembly: Purview.EventSourcing.Aggregates.AggregateDefaults(EventSuffix = ""DomainEvent"")]

namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate(EventSuffix = ""CustomEvent"")]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(generatedSource).Contains("public sealed class OrderCreatedCustomEvent");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.OrderCreatedCustomEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("void Apply(global::Testing.OrderEvents.OrderCreatedCustomEvent @event)");
	}

	[Test]
	public async Task Generate_GivenAggregateEventNamespaceOverride_UsesConfiguredNamespace(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate(EventNamespace = ""Testing.Custom.Events"")]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(generatedSource).Contains("namespace Testing.Custom.Events");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.Custom.Events.OrderCreatedEvent>(Apply);");
	}

	[Test]
	public async Task Generate_GivenEventNameAndNamespaceOverride_UsesMethodOverrides(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate(EventNamespace = ""Testing.Custom.Events"")]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event(EventName = ""OrderCreated"", EventNamespace = ""Testing.Domain.Ordering"")]
		public partial void CreateOrder(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(generatedSource).Contains("namespace Testing.Domain.Ordering");
		await Assert.That(generatedSource).Contains("public sealed class OrderCreated");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.Domain.Ordering.OrderCreated>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("void Apply(global::Testing.Domain.Ordering.OrderCreated @event)");
	}

	[Test]
	public async Task Generate_GivenFalsePositiveAggregateBaseName_ReportsInheritanceDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public abstract class AggregateBase { }

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class NotARealAggregate : AggregateBase
	{
		[Purview.EventSourcing.Aggregates.Event]
		public partial void Rename(string name);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic("EVENTSTORE002");
	}

	[Test]
	public async Task Generate_GivenNestedAggregate_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public static class AggregateContainer
	{
		[Purview.EventSourcing.Aggregates.Aggregate]
		public partial class NestedAggregate : Purview.EventSourcing.Aggregates.AggregateBase
		{
			public string Value { get; private set; } = default!;

			[Purview.EventSourcing.Aggregates.Event]
			public partial void SetValue(string value);
		}
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic("EVENTSTORE003");
	}

	[Test]
	public async Task Generate_GivenGenericAggregate_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class GenericAggregate<TValue> : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public TValue Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(TValue value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic("EVENTSTORE004");
	}

	[Test]
	public async Task Generate_GivenManualRegisterEvents_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ManualRegistrationAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		protected override void RegisterEvents() { }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic("EVENTSTORE005");
	}

	[Test]
	[Arguments("set")]
	[Arguments("protected set")]
	[Arguments("internal set")]
	[Arguments("protected internal set")]
	[Arguments("private protected set")]
	public async Task Generate_GivenPropertySetterIsNotPrivate_ReportsError(
		string setterAccess,
		CancellationToken cancellationToken
	)
	{
		var source =
			$@"
namespace Testing
{{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class PublicSetterAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{{
		public string Value {{ get; {setterAccess}; }} = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(string value);
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic("EVENTSTORE011");
		await Assert
			.That(
				result
					.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
					.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.Select(static diagnostic => diagnostic.Id)
			)
			.DoesNotContain("CS8795");
	}

	[Test]
	public async Task Generate_GivenEventMethodOutsideAggregate_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public partial class UtilityType
	{
		[Purview.EventSourcing.Aggregates.Event]
		public partial void DoWork(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic("EVENTSTORE006");
	}

	[Test]
	public async Task Generate_GivenNonAggregateReturnTypeEventMethod_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class InvalidSignatureAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial string SetValue(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(result).HasDiagnostic("EVENTSTORE008");
		await Assert.That(generatedSource).Contains("public partial string SetValue(string value)");
		await Assert
			.That(
				result
					.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
					.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.Select(static diagnostic => diagnostic.Id)
			)
			.DoesNotContain("CS8795");
	}

	[Test]
	public async Task Generate_GivenUnsupportedEventMethodSignature_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class InvalidSignatureAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public static partial string SetValue(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(result).HasDiagnostic("EVENTSTORE008");
		await Assert
			.That(generatedSource)
			.Contains("public static partial string SetValue(string value)");
		await Assert
			.That(generatedSource)
			.Contains(
				"The generated aggregate event method 'public static partial string SetValue(string value)' is unavailable because [Event] validation failed. Review the suppressed generator diagnostics for this method (EVENTSTORE008)."
			);
		await Assert
			.That(
				result
					.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
					.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.Select(static diagnostic => diagnostic.Id)
			)
			.DoesNotContain("CS8795");
	}

	[Test]
	public async Task Generate_GivenOverloadedEventMethods_ReportsDuplicateEventNameDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class DuplicateEventAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;
		public int Count { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Update(string value);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Update(int count);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert.That(result).HasDiagnostic("EVENTSTORE009");
		await Assert.That(generatedSource).Contains("public partial void Update(string value)");
		await Assert.That(generatedSource).Contains("public partial void Update(int count)");
		await Assert.That(generatedSource).Contains("EVENTSTORE009");
		await Assert
			.That(
				result
					.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
					.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.Select(static diagnostic => diagnostic.Id)
			)
			.DoesNotContain("CS8795");
	}

	[Test]
	public async Task Generate_GivenMissingPropertyMapping_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class MappingAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Rename(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(DiagnosticLibrary.EventParameterMustMapToWritableProperty);
	}

	[Test]
	public async Task Generate_GivenMetadataStoreTrue_AddsStoredEventPropertyWithoutAggregateMutation(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class MappingAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Rename([Purview.EventSourcing.Aggregates.Metadata] string initialPropertyToTest);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.EventParameterMustMapToWritableProperty);
		await Assert
			.That(generatedSource)
			.Contains("public partial void Rename(string initialPropertyToTest)");
		await Assert
			.That(generatedSource)
			.Contains("public string InitialPropertyToTest { get; set; } = default!;");
		await Assert
			.That(generatedSource)
			.Contains("OnRaisingRenamedEvent(ref initialPropertyToTest);");
		await Assert
			.That(generatedSource)
			.Contains("InitialPropertyToTest = initialPropertyToTest,");
		await Assert.That(generatedSource).Contains("OnRaisedRenamedEvent(@event);");
		await Assert
			.That(
				result
					.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
					.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.Select(static diagnostic => diagnostic.Id)
			)
			.DoesNotContain("CS8795");
	}

	[Test]
	public async Task Generate_GivenMetadataStoreFalse_PassesParameterToOnRaisingWithoutStoringAndStoring(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class MappingAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Rename(
			[Purview.EventSourcing.Aggregates.Metadata(false)] string correlationId,
			[Purview.EventSourcing.Aggregates.Metadata] string correlationToStoreImplicitId,
			[Purview.EventSourcing.Aggregates.Metadata(true)] string? correlationToStoreExplicitId
		);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.EventParameterMustMapToWritableProperty);
		await Assert
			.That(generatedSource)
			.Contains(
				"public partial void Rename(string correlationId, string correlationToStoreImplicitId, string? correlationToStoreExplicitId)"
			);
		await Assert
			.That(generatedSource)
			.Contains(
				"OnRaisingRenamedEvent(ref string correlationId, ref string correlationToStoreImplicitId, ref string? correlationToStoreExplicitId);"
			);
		await Assert
			.That(generatedSource)
			.DoesNotContain("public string CorrelationId { get; set; } = default!;");
		await Assert
			.That(generatedSource)
			.Contains("public string CorrelationToStoreImplicitId { get; set; } = default!;");
		await Assert
			.That(generatedSource)
			.Contains("public string? CorrelationToStoreExplicitId { get; set; } = default!;");
		await Assert
			.That(generatedSource)
			.Contains("var @event = new global::Testing.MappingEvents.Renamed");
		await Assert
			.That(
				result
					.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
					.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.Select(static diagnostic => diagnostic.Id)
			)
			.DoesNotContain("CS8795");
	}

	[Test]
	public async Task Generate_GivenPropertyOverride_MapsParameterToSpecifiedProperty(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class MappingAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public int QuantityOnHand { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void ReceiveStock([Purview.EventSourcing.Aggregates.Property(nameof(QuantityOnHand))] int initialQuantity);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(generatedSource)
			.Contains("public int InitialQuantity { get; set; } = default!;");
		await Assert.That(generatedSource).Contains("QuantityOnHand = @event.InitialQuantity;");
		await Assert
			.That(generatedSource)
			.Contains("OnRaisingStockReceivedEvent(ref initialQuantity);");
		await Assert
			.That(
				result
					.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
					.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.Select(static diagnostic => diagnostic.Id)
			)
			.DoesNotContain("CS8795");
	}

	[Test]
	public async Task Generate_GivenPropertyOverrideTargetMissing_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class MappingAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public int QuantityOnHand { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void ReceiveStock([Purview.EventSourcing.Aggregates.Property(""MissingProperty"")] int initialQuantity);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(DiagnosticLibrary.EventParameterMustMapToWritableProperty);
	}

	[Test]
	public async Task Generate_GivenInitOnlyMappedProperty_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class InitOnlyAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; init; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic("EVENTSTORE010");
	}

	[Test]
	public async Task Generate_GivenSameAggregateNameInDifferentNamespaces_UsesUniqueHintNames(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace First
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(string value);
	}
}

namespace Second
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetValue(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var aggregateTrees = result.PrimarySyntaxTrees;

		await Assert.That(aggregateTrees.Length).IsEqualTo(2);
	}

	[Test]
	public async Task Generate_GivenAggregateWithManyEvents_GeneratesAllEventsAndRegistrations(
		CancellationToken cancellationToken
	)
	{
		// Arrange ÔÇö aggregate with 5 events covering full lifecycle
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }
		public decimal Total { get; private set; }
		public string Status { get; private set; }
		public string ShippingAddress { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void UpdateTotal(decimal total);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetShippingAddress(string shippingAddress);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void ConfirmOrder();

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CancelOrder();

		partial void Apply(global::Testing.OrderEvents.OrderConfirmedEvent @event)
		{
			Status = ""Confirmed"";
		}

		partial void Apply(global::Testing.OrderEvents.OrderCanceledEvent @event)
		{
			Status = ""Canceled"";
		}
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that all 5 event classes
		await Assert.That(generatedSource).Contains("public sealed class OrderCreated");
		await Assert.That(generatedSource).Contains("public sealed class TotalUpdated");
		await Assert.That(generatedSource).Contains("public sealed class ShippingAddressSetEvent");
		await Assert.That(generatedSource).Contains("public sealed class OrderConfirmedEvent");
		await Assert.That(generatedSource).Contains("public sealed class OrderCanceledEvent"); // US spelling

		// Assert that all 5 Register calls
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.OrderCreatedEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.TotalUpdatedEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.ShippingAddressSetEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.OrderConfirmedEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.OrderEvents.OrderCanceledEvent>(Apply);");

		// Assert that compiles without errors
		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenEventWithMultipleParameterTypes_GeneratesCorrectProperties(
		CancellationToken cancellationToken
	)
	{
		// Arrange ÔÇö event with int, decimal, string, bool parameters
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ProductAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Name { get; private set; }
		public decimal Price { get; private set; }
		public int Quantity { get; private set; }
		public bool IsAvailable { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void UpdateProduct(string name, decimal price, int quantity, bool isAvailable);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that all parameter types are represented as properties
		await Assert.That(generatedSource).Contains("public string Name { get; set; }");
		await Assert.That(generatedSource).Contains("public decimal Price { get; set; }");
		await Assert.That(generatedSource).Contains("public int Quantity { get; set; }");
		await Assert.That(generatedSource).Contains("public bool IsAvailable { get; set; }");

		// Assert that Apply method sets all properties
		await Assert.That(generatedSource).Contains("Name = @event.Name;");
		await Assert.That(generatedSource).Contains("Price = @event.Price;");
		await Assert.That(generatedSource).Contains("Quantity = @event.Quantity;");
		await Assert.That(generatedSource).Contains("IsAvailable = @event.IsAvailable;");

		// Assert that BuildEventHash includes all properties
		await Assert.That(generatedSource).Contains("hash.Add(Name);");
		await Assert.That(generatedSource).Contains("hash.Add(Price);");
		await Assert.That(generatedSource).Contains("hash.Add(Quantity);");
		await Assert.That(generatedSource).Contains("hash.Add(IsAvailable);");
	}

	[Test]
	public async Task Generate_GivenAggregateWithTransitiveInheritance_GeneratesCode(
		CancellationToken cancellationToken
	)
	{
		// Arrange ÔÇö aggregate inherits through an intermediate base class
		const string source =
			@"
namespace Testing
{
	public abstract class DomainAggregateBase : Purview.EventSourcing.Aggregates.AggregateBase
	{
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class AccountAggregate : DomainAggregateBase
	{
		public string AccountName { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateAccount(string accountName);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert that attribute files + 1 generated aggregate
		await Assert
			.That(result.DriverResult.GeneratedTrees)
			.Count()
			.IsEqualTo(ExpectedFileCountPlusGen);

		var generatedSource = result.GetSource();
		await Assert.That(generatedSource).Contains("public sealed class AccountCreatedEvent");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.AccountEvents.AccountCreatedEvent>(Apply);");
	}

	[Test]
	public async Task Generate_GivenAggregateWithMultiLevelTransitiveInheritance_GeneratesCode(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public abstract class DomainAggregateBase : Purview.EventSourcing.Aggregates.AggregateBase
	{
	}

	public abstract class BillingAggregateBase : DomainAggregateBase
	{
	}

	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class InvoiceAggregate : BillingAggregateBase
	{
		public string InvoiceNumber { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateInvoice(string invoiceNumber);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(result.DriverResult.GeneratedTrees)
			.Count()
			.IsEqualTo(ExpectedFileCountPlusGen);
		await Assert.That(generatedSource).Contains("public sealed class InvoiceCreatedEvent");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.InvoiceEvents.InvoiceCreatedEvent>(Apply);");
		await Assert.That(result).DoesNotHaveDiagnostic("EVENTSTORE002");
	}

	[Test]
	public async Task Generate_GivenNestedNamespace_GeneratesCorrectEventsNamespace(
		CancellationToken cancellationToken
	)
	{
		// Arrange ÔÇö deeply nested namespace
		const string source =
			@"
namespace Company.Domain.Orders
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that events namespace follows the pattern
		await Assert.That(generatedSource).Contains("namespace Company.Domain.Orders.OrderEvents");
		await Assert
			.That(generatedSource)
			.Contains(
				"Register<global::Company.Domain.Orders.OrderEvents.OrderCreatedEvent>(Apply);"
			);
		await Assert.That(generatedSource).Contains("namespace Company.Domain.Orders");
	}

	[Test]
	public async Task Generate_GivenParameterlessAndParameterizedEvents_GeneratesBothCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange ÔÇö mix of parameterless and parameterized events
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class CounterAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public int Count { get; private set; }
		public string Label { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Increment();

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Decrement();

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetLabel(string label);

		[Purview.EventSourcing.Aggregates.Event]
		public partial void Reset();

		partial void Apply(global::Testing.CounterEvents.IncrementedEvent @event) => Count++;
		partial void Apply(global::Testing.CounterEvents.DecrementedEvent @event) => Count--;
		partial void Apply(global::Testing.CounterEvents.ResetEvent @event) => Count = 0;
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that parameterless use () constructor, parameterized use { } initializer
		await Assert
			.That(generatedSource)
			.Contains("var @event = new global::Testing.CounterEvents.IncrementedEvent");
		await Assert
			.That(generatedSource)
			.Contains("var @event = new global::Testing.CounterEvents.DecrementedEvent");
		await Assert
			.That(generatedSource)
			.Contains("var @event = new global::Testing.CounterEvents.ResetEvent");
		await Assert.That(generatedSource).Contains("RecordAndApply(@event);");
		await Assert.That(generatedSource).Contains("Label = label,");

		// Assert that all 4 Register calls
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.CounterEvents.IncrementedEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.CounterEvents.DecrementedEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.CounterEvents.LabelSetEvent>(Apply);");
		await Assert
			.That(generatedSource)
			.Contains("Register<global::Testing.CounterEvents.ResetEvent>(Apply);");

		// Assert that compiles without errors
		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();
		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenNullableParameter_GeneratesNullableProperty(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ProfileAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string? Bio { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void UpdateBio(string? bio);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that nullable parameter generates property and Apply method
		await Assert.That(generatedSource).Contains("Bio");
		await Assert.That(generatedSource).Contains("Bio = @event.Bio;");
		await Assert.That(generatedSource).Contains("public sealed class BioUpdated");
		await Assert.That(generatedSource).Contains("public string? Bio { get; set; } = default!;");
		await Assert.That(generatedSource).Contains("public partial void UpdateBio(string? bio)");
	}

	[Test]
	public async Task Generate_GivenNotNullParameter_GeneratesGuard(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;

			namespace Testing
			{
				[Purview.EventSourcing.Aggregates.Aggregate]
				public partial class ProfileAggregate : Purview.EventSourcing.Aggregates.AggregateBase
				{
					public string? Bio { get; private set; }

					[Purview.EventSourcing.Aggregates.Event]
					public partial void UpdateBio([NotNull] string? bio);
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();
		var cs8777 = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Id == "CS8777")
			.ToArray();

		await Assert.That(cs8777).IsEmpty();
		await Assert.That(generatedSource).Contains("public string Bio { get; set; } = default!;");
		await Assert.That(generatedSource).Contains("var __bioValue = bio!;");
		await Assert.That(generatedSource).Contains("if (bio is null)");
		await Assert
			.That(generatedSource)
			.Contains("throw new global::System.ArgumentNullException(nameof(bio));");
		await Assert.That(generatedSource).Contains("OnBioChanging(ref __bioValue);");
		await Assert.That(generatedSource).Contains("Bio = __bioValue!,");
	}

	[Test]
	public async Task Generate_GivenRequiredStringParameter_GeneratesGuard(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.ComponentModel.DataAnnotations;

			namespace Testing
			{
				[Purview.EventSourcing.Aggregates.Aggregate]
				public partial class ProfileAggregate : Purview.EventSourcing.Aggregates.AggregateBase
				{
					public string? Bio { get; private set; }

					[Purview.EventSourcing.Aggregates.Event]
					public partial void UpdateBio([Required] string? bio);
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();
		var cs8777 = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(d => d.Id == "CS8777")
			.ToArray();

		await Assert.That(cs8777).IsEmpty();
		await Assert.That(generatedSource).Contains("public string Bio { get; set; } = default!;");
		await Assert.That(generatedSource).Contains("var __bioValue = bio!;");
		await Assert
			.That(generatedSource)
			.Contains("if (global::System.String.IsNullOrWhiteSpace(bio))");
		await Assert
			.That(generatedSource)
			.Contains(
				"throw new global::System.ArgumentException(\"Parameter 'bio' cannot be null or empty.\", nameof(bio));"
			);
		await Assert.That(generatedSource).Contains("OnBioChanging(ref __bioValue);");
		await Assert.That(generatedSource).Contains("Bio = __bioValue!,");
	}

	[Test]
	public async Task Generate_GivenPublicAccessibility_GeneratesPublicPartialClass(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class PublicAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		[Purview.EventSourcing.Aggregates.Event]
		public partial void DoAction();
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert
		await Assert.That(generatedSource).Contains("public partial class PublicAggregate");
	}

	[Test]
	public async Task Generate_GivenEventWithSingleParameter_CommandMethodHasCorrectSignature(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class NoteAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Content { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetContent(string content);
	}
}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		// Assert that command method matches partial declaration
		await Assert
			.That(generatedSource)
			.Contains("public partial void SetContent(string content)");
		// Assert that RecordAndApply creates event with property
		await Assert.That(generatedSource).Contains("Content = content,");
	}

	[Test]
	public async Task Generate_GivenGeneratedEvent_InvokesShouldApplyBeforeAndAfterOnRaising(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class CustomerAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetCustomerId(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		var firstShouldApplyIndex = generatedSource.IndexOf(
			"if (!ShouldApplyCustomerIdSetEvent(@event))",
			StringComparison.Ordinal
		);
		var onRaisingIndex = generatedSource.IndexOf(
			"OnRaisingCustomerIdSetEvent(ref customerId);",
			StringComparison.Ordinal
		);
		var secondShouldApplyIndex = generatedSource.IndexOf(
			"if (!ShouldApplyCustomerIdSetEvent(@event))",
			firstShouldApplyIndex + 1,
			StringComparison.Ordinal
		);

		await Assert.That(firstShouldApplyIndex).IsGreaterThanOrEqualTo(0);
		await Assert.That(onRaisingIndex).IsGreaterThanOrEqualTo(0);
		await Assert.That(secondShouldApplyIndex).IsGreaterThanOrEqualTo(0);
		await Assert.That(firstShouldApplyIndex).IsLessThan(onRaisingIndex);
		await Assert.That(onRaisingIndex).IsLessThan(secondShouldApplyIndex);
		await Assert
			.That(generatedSource)
			.Contains(
				"partial void OnShouldApplyCustomerIdSetEvent(global::Testing.CustomerEvents.CustomerIdSetEvent @event, ref bool shouldApply);"
			);
	}

	[Test]
	public async Task Generate_GivenSecondShouldApplyReturnsFalse_StopsProcessingAndReturnsToCaller(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class CustomerAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public int ShouldApplyCallCount { get; private set; }
		public string? LastRaisedValue { get; private set; }
		public string? CustomerId { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial bool SetCustomerId(string customerId);

		partial void OnRaisingCustomerIdSetEvent(ref string customerId)
		{
			customerId = ""raised-value"";
			LastRaisedValue = customerId;
		}

		partial void OnShouldApplyCustomerIdSetEvent(global::Testing.CustomerEvents.CustomerIdSetEvent @event, ref bool shouldApply)
		{
			ShouldApplyCallCount++;
			shouldApply = ShouldApplyCallCount == 1;
		}
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var aggregateType = assembly.GetType("Testing.CustomerAggregate")!;
		var instance = Activator.CreateInstance(aggregateType)!;
		var setCustomerId = aggregateType.GetMethod("SetCustomerId")!;

		var setResult = (bool)setCustomerId.Invoke(instance, ["input-value"])!;

		await Assert.That(setResult).IsFalse();
		await Assert
			.That(aggregateType.GetProperty("ShouldApplyCallCount")!.GetValue(instance))
			.IsEqualTo(2);
		await Assert
			.That(aggregateType.GetProperty("LastRaisedValue")!.GetValue(instance))
			.IsEqualTo("raised-value");
		await Assert.That(aggregateType.GetProperty("CustomerId")!.GetValue(instance)).IsNull();
	}

	[Test]
	public async Task Generate_GivenNullValidationHook_RunsValidationBeforeNoChangeGuard(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class CustomerAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void SetCustomerId(string customerId);

		partial void OnCustomerIdChanging(ref string customerId) => global::System.ArgumentNullException.ThrowIfNull(customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		var onChangingIndex = generatedSource.IndexOf(
			"OnCustomerIdChanging(ref customerId);",
			StringComparison.Ordinal
		);
		var onRaisingIndex = generatedSource.IndexOf(
			"OnRaisingCustomerIdSetEvent(ref customerId);",
			StringComparison.Ordinal
		);
		var noChangeIndex = generatedSource.IndexOf(
			"if (global::System.String.Equals(CustomerId, customerId, global::System.StringComparison.Ordinal))",
			StringComparison.Ordinal
		);

		await Assert.That(onChangingIndex).IsGreaterThanOrEqualTo(0);
		await Assert.That(onRaisingIndex).IsGreaterThanOrEqualTo(0);
		await Assert.That(noChangeIndex).IsGreaterThanOrEqualTo(0);
		await Assert.That(onChangingIndex).IsLessThan(noChangeIndex);
		await Assert.That(onRaisingIndex).IsLessThan(noChangeIndex);

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var aggregateType = assembly.GetType("Testing.CustomerAggregate")!;
		var instance = Activator.CreateInstance(aggregateType)!;
		var setCustomerId = aggregateType.GetMethod("SetCustomerId")!;
		var threwArgumentNullException = false;

		try
		{
			setCustomerId.Invoke(instance, [null]);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is ArgumentNullException)
		{
			threwArgumentNullException = true;
		}

		await Assert.That(threwArgumentNullException).IsTrue();
	}

	[Test]
	public async Task Generate_GivenSimpleAggregate_CanRoundTripPrivateSetterStateWithSystemTextJson(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = string.Empty;
		public decimal Total { get; private set; }

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId, decimal total);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var aggregateType = assembly.GetType("Testing.OrderAggregate")!;
		var instance = Activator.CreateInstance(aggregateType)!;
		aggregateType.GetMethod("CreateOrder")!.Invoke(instance, ["customer-1", 12.5m]);

		var detailsProperty = aggregateType.GetProperty("Details")!;
		var detailsType = detailsProperty.PropertyType;
		var details = Activator.CreateInstance(detailsType)!;
		detailsType.GetProperty("Id")!.SetValue(details, "aggregate-1");
		detailsProperty.SetValue(instance, details);

		var json = JsonSerializer.Serialize(instance, aggregateType);
		var roundTripped = JsonSerializer.Deserialize(json, aggregateType)!;

		await Assert
			.That(aggregateType.GetProperty("CustomerId")!.GetValue(roundTripped))
			.IsEqualTo("customer-1");
		await Assert
			.That(aggregateType.GetProperty("Total")!.GetValue(roundTripped))
			.IsEqualTo(12.5m);
		var roundTrippedDetails = detailsProperty.GetValue(roundTripped)!;
		await Assert
			.That(detailsType.GetProperty("Id")!.GetValue(roundTrippedDetails))
			.IsEqualTo("aggregate-1");
	}

	[Test]
	public async Task Generate_GivenGeneratedEvent_CanRoundTripEventDetailsWithSystemTextJson(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class OrderAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string CustomerId { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event]
		public partial void CreateOrder(string customerId);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var eventType = assembly.GetType("Testing.OrderEvents.OrderCreatedEvent")!;
		var instance = Activator.CreateInstance(eventType)!;
		eventType.GetProperty("CustomerId")!.SetValue(instance, "customer-2");

		var detailsProperty = eventType.GetProperty(
			"Details",
			BindingFlags.Public | BindingFlags.Instance
		)!;
		var detailsType = detailsProperty.PropertyType;
		var details = Activator.CreateInstance(detailsType)!;
		detailsType.GetProperty("CorrelationId")!.SetValue(details, "corr-1");
		detailsProperty.SetValue(instance, details);

		var json = JsonSerializer.Serialize(instance, eventType);
		var roundTripped = JsonSerializer.Deserialize(json, eventType)!;

		await Assert
			.That(eventType.GetProperty("CustomerId")!.GetValue(roundTripped))
			.IsEqualTo("customer-2");
		var roundTrippedDetails = detailsProperty.GetValue(roundTripped)!;
		await Assert
			.That(detailsType.GetProperty("CorrelationId")!.GetValue(roundTrippedDetails))
			.IsEqualTo("corr-1");
	}

	[Test]
	public async Task Generate_GivenSuspiciousMethodNames_ReportsVerbPhraseDiagnostics(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class NamingAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event(EventName = ""CustomerRegistered"")]
		public partial void NewCustomer(string value);

		[Purview.EventSourcing.Aggregates.Event(EventName = ""CustomerCreated"")]
		public partial void CustomerRegistered(string value);

		[Purview.EventSourcing.Aggregates.Event(EventName = ""ValueChanged"")]
		public partial void NameChanged(string value);

		[Purview.EventSourcing.Aggregates.Event(EventName = ""ValueSet"")]
		public partial void Handle(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var warnings = result
			.DriverResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning)
			.ToArray();

		await Assert
			.That(
				warnings.Count(d => d.Id == DiagnosticLibrary.AggregateMethodShouldBeVerbPhrase.Id)
			)
			.IsEqualTo(4);

		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.UnableToInferEventName);
		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.EventNameOverrideShouldBePastTense);
	}

	[Test]
	public async Task Generate_GivenInvalidEventNameOverrides_ReportsPastTenseDiagnostics(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class NamingAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = default!;

		[Purview.EventSourcing.Aggregates.Event(EventName = ""RegisterCustomer"")]
		public partial void NewCustomer(string value);

		[Purview.EventSourcing.Aggregates.Event(EventName = ""CreateCustomer"")]
		public partial void CreateCustomer(string value);

		[Purview.EventSourcing.Aggregates.Event(EventName = ""ApproveQuestion"")]
		public partial void ApproveQuestion(string value);

		[Purview.EventSourcing.Aggregates.Event(EventName = ""WithdrawConsent"")]
		public partial void WithdrawConsent(string value);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var warnings = result
			.DriverResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning)
			.ToArray();

		await Assert.That(warnings.Count(d => d.Id == "EVENTSTORE014")).IsEqualTo(4);
	}

	[Test]
	public async Task Generate_GivenManualEventTypes_ReportsPastTenseDiagnostics(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	public sealed class NameChanged : Purview.EventSourcing.Aggregates.Events.EventBase
	{
		protected override void BuildEventHash(ref global::System.HashCode hash)
		{
		}
	}

	public sealed record ChangeName : Purview.EventSourcing.Aggregates.Events.EventBase
	{
		protected override void BuildEventHash(ref global::System.HashCode hash)
		{
		}
	}

	public sealed record class CustomerRegisteredEvent : Purview.EventSourcing.Aggregates.Events.EventBase
	{
		protected override void BuildEventHash(ref global::System.HashCode hash)
		{
		}
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var warnings = result
			.DriverResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning)
			.ToArray();

		await Assert
			.That(warnings.Select(d => d.Id))
			.Contains(DiagnosticLibrary.EventNameShouldBePastTense.Id);
		await Assert
			.That(warnings.Count(d => d.Id == DiagnosticLibrary.EventNameShouldBePastTense.Id))
			.IsEqualTo(1);
		await Assert
			.That(warnings.Select(d => d.Id))
			.DoesNotContain(DiagnosticLibrary.EventNameOverrideShouldBePastTense.Id);
		await Assert
			.That(warnings.Select(d => d.Id))
			.DoesNotContain(DiagnosticLibrary.UnableToInferEventName.Id);
	}

	[Test]
	public async Task Generate_GivenNonNullableParamMappedToNullableProperty_EmitsNullabilityMismatchWarning(
		CancellationToken cancellationToken
	)
	{
		// Non-nullable string parameter mapping to a nullable string? aggregate property.
		// The generator handles it automatically (adds a local cast), but also emits EVENTSTORE016
		// to guide the developer to fix the signature rather than rely on the workaround.
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class NoteAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
			public string? Note { get; private set; }

			[Purview.EventSourcing.Aggregates.Event]
			public partial void SetNote(string note);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var warnings = result
			.DriverResult.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Info)
			.ToArray();

		await Assert
			.That(warnings.Select(static d => d.Id))
			.Contains(DiagnosticLibrary.EventParameterNullabilityMismatch.Id);

		var mismatchWarning = warnings.First(static d =>
			d.Id == DiagnosticLibrary.EventParameterNullabilityMismatch.Id
		);
		var message = mismatchWarning.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
		await Assert.That(message).Contains("note");
		await Assert.That(message).Contains("SetNote");
		await Assert.That(message).Contains("Note");
		await Assert.That(message).Contains("string?");
	}

	[Test]
	public async Task Generate_GivenNullableParamMappedToNullableProperty_DoesNotEmitNullabilityMismatchWarning(
		CancellationToken cancellationToken
	)
	{
		// When the parameter already has the matching nullable annotation, no warning should be emitted.
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class NoteAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
			public string? Note { get; private set; }

			[Purview.EventSourcing.Aggregates.Event]
			public partial void SetNote(string? note);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var warnings = result
			.DriverResult.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Warning)
			.ToArray();

		await Assert
			.That(warnings.Select(static d => d.Id))
			.DoesNotContain(DiagnosticLibrary.EventParameterNullabilityMismatch.Id);
	}

	[Test]
	[Arguments("System.Collections.Generic.List<string>")]
	[Arguments("System.Collections.Generic.IList<string>")]
	[Arguments("System.Collections.Generic.ICollection<string>")]
	[Arguments("System.Collections.Generic.IReadOnlyList<string>")]
	[Arguments("System.Collections.Generic.IReadOnlyCollection<string>")]
	[Arguments("System.Collections.Generic.IEnumerable<string>")]
	[Arguments("System.Collections.Generic.HashSet<string>")]
	[Arguments("string[]")]
	public async Task Generate_GivenNonEventStoreCollectionProperty_ReportsCollectionTypeError(
		string collectionType,
		CancellationToken cancellationToken
	)
	{
		var source =
			$@"namespace Testing;

[Aggregate]
public partial class ItemAggregate : AggregateBase
{{
	public {collectionType} Tags {{ get; private set; }}

	[Event]
	public partial void SetTags({collectionType} tags);
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(
				DiagnosticLibrary.AggregatePropertyCollectionTypeMustUseEventStoreCollections
			);
	}

	[Test]
	[Arguments("EventStoreList<string>")]
	[Arguments("EventStoreSet<string>")]
	public async Task Generate_GivenEventStoreCollectionProperty_DoesNotReportCollectionTypeError(
		string collectionType,
		CancellationToken cancellationToken
	)
	{
		var source =
			$@"namespace Testing;

[Aggregate]
public partial class ItemAggregate : AggregateBase
{{
	public {collectionType} Tags {{ get; private set; }} = new();

	[Event]
	public partial void SetTags({collectionType} tags);
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(
				DiagnosticLibrary.AggregatePropertyCollectionTypeMustUseEventStoreCollections
			);
	}

	[Test]
	public async Task Generate_GivenCollectionEvents_UsesCollectionSemanticsAndSharedEnumerableHooks(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ItemAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public Purview.EventSourcing.EventStoreSet<string> Tags { get; private set; } = [];

		[Purview.EventSourcing.Aggregates.CollectionEvent(nameof(Tags))]
		public partial ItemAggregate AddTag(string tag);

		[Purview.EventSourcing.Aggregates.CollectionEvent(nameof(Tags))]
		public partial ItemAggregate AddTags(System.Collections.Generic.IEnumerable<string> tags);

		[Purview.EventSourcing.Aggregates.CollectionEvent(nameof(Tags))]
		public partial ItemAggregate AddTags(params string[] tags);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();
		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.UnsupportedEventMethodSignature);
		await Assert
			.That(generatedSource)
			.Contains(
				"partial void OnNormalizingAddTags(ref global::System.Collections.Generic.IEnumerable<string> tags);"
			);
		await Assert
			.That(generatedSource)
			.Contains(
				"partial void OnValidatingAddTags(global::System.Collections.Generic.IEnumerable<string> tags);"
			);
		await Assert.That(generatedSource).Contains("if (Tags.Contains(__itemValue))");
		await Assert
			.That(generatedSource)
			.Contains("var __eventItems = __itemsValue as string[] ?? [.. __itemsValue];");
		await Assert
			.That(generatedSource)
			.Contains(
				"((global::System.Collections.Generic.ICollection<string>)Tags).Add(__item);"
			);

		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(static d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();
		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task Generate_GivenManualEventAttribute_DisablesAutomaticApplyAndRequiresImplementation(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ManualAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public string Value { get; private set; } = string.Empty;

		[Purview.EventSourcing.Aggregates.Event(EventName = ""ValueCommandAppliedEvent"", Manual = true)]
		public partial void ApplyValueCommand(string input);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(generatedSource)
			.Contains(
				"private partial void Apply(global::Testing.ManualEvents.ValueCommandAppliedEvent @event);"
			);
		await Assert.That(generatedSource).DoesNotContain("Value = @event.Input;");

		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(static d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();
		await Assert.That(errors.Select(static d => d.Id)).Contains("CS8795");
	}

	[Test]
	public async Task Generate_GivenManualCollectionEventAttribute_DisablesAutomaticApplyAndRequiresImplementation(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ManualCollectionAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public Purview.EventSourcing.EventStoreList<string> Tags { get; private set; } = [];

		[Purview.EventSourcing.Aggregates.CollectionEvent(nameof(Tags), Manual = true)]
		public partial void AddTag(string tag);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource();

		await Assert
			.That(generatedSource)
			.Contains(
				"private partial void Apply(global::Testing.ManualCollectionEvents.TagAddedEvent @event);"
			);
		await Assert
			.That(generatedSource)
			.DoesNotContain("((global::System.Collections.Generic.ICollection<string>)Tags).Add(");

		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(static d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();
		await Assert.That(errors.Select(static d => d.Id)).Contains("CS8795");
	}

	[Test]
	public async Task Generate_GivenCollectionRemoveMethodName_InfersRemoveMutationAndSkipsNoChangeEvents(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ItemAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public Purview.EventSourcing.EventStoreSet<string> Tags { get; private set; } = [];

		[Purview.EventSourcing.Aggregates.CollectionEvent(nameof(Tags))]
		public partial ItemAggregate RemoveTag(string tag);
	}
}
";

		var result = await GenerateAsync(source, cancellationToken);
		var generatedSource = result.GetSource("ItemAggregate", HintNameMatchMode.Partial);

		await Assert.That(generatedSource).IsNotNull();

		await Assert
			.That(result)
			.DoesNotHaveDiagnostic(DiagnosticLibrary.UnsupportedEventMethodSignature);
		await Assert.That(generatedSource).Contains("if (!Tags.Contains(__itemValue))");
		await Assert
			.That(generatedSource)
			.Contains(
				"((global::System.Collections.Generic.ICollection<string>)Tags).Remove(@event.Tag);"
			);
	}

	[Test]
	public async Task Generate_GivenCollectionOperationOverride_UsesSpecifiedMutation(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[Purview.EventSourcing.Aggregates.Aggregate]
	public partial class ItemAggregate : Purview.EventSourcing.Aggregates.AggregateBase
	{
		public Purview.EventSourcing.EventStoreSet<string> Tags { get; private set; } = [];

		[Purview.EventSourcing.Aggregates.CollectionEvent(nameof(Tags), Operation = Purview.EventSourcing.Aggregates.CollectionEventOperation.Remove)]
		public partial ItemAggregate ArchiveTag(string tag);

		[Purview.EventSourcing.Aggregates.CollectionEvent(nameof(Tags), Operation = Purview.EventSourcing.Aggregates.CollectionEventOperation.Add)]
		public partial ItemAggregate DeleteTag(string tag);
	}
}
";

		var options = new EventSourcingGeneratorTestOptions
		{
			IncludeDefaultNamespaces = false,
			AdditionalNamespaces = [],
		};
		var result = await GenerateAsync(source, options, cancellationToken);
		var diagnostics = result.DriverResult.Diagnostics.Select(static d => d.Id).ToArray();
		var generatedSource = result.GetSource();
		await Assert
			.That(diagnostics)
			.DoesNotContain(DiagnosticLibrary.UnsupportedEventMethodSignature.Id);
		await Assert.That(generatedSource).Contains("if (!Tags.Contains(__itemValue))");
		await Assert.That(generatedSource).Contains("if (Tags.Contains(__itemValue))");
		await Assert
			.That(generatedSource)
			.Contains(
				"((global::System.Collections.Generic.ICollection<string>)Tags).Remove(@event.Tag);"
			);
		await Assert
			.That(generatedSource)
			.Contains(
				"((global::System.Collections.Generic.ICollection<string>)Tags).Add(@event.Tag);"
			);

		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(static d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();
		await Assert.That(errors).IsEmpty();
	}
}
