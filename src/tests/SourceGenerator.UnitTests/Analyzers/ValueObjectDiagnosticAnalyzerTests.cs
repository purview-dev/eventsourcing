using Purview.SourceGeneratorFramework;

namespace Purview.EventSourcing.SourceGenerator.Analyzers;

public sealed class ValueObjectDiagnosticAnalyzerTests : AnalyzerTestBase<ValueObjectDiagnosticAnalyzer>
{
	[Test]
	public async Task Generate_GivenNonPartialValueObject_ReportsValueObjectMustBePartial(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Scalar]
				public readonly record struct EmailAddress
				{
					public string Value { get; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ValueObjectMustBePartial);
	}

	[Test]
	public async Task Generate_GivenNestedValueObject_ReportsNestedValueObjectsAreNotSupported(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				public class Outer
				{
					[Scalar]
					public readonly partial record struct EmailAddress
					{
						public string Value { get; }
					}
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.NestedValueObjectsAreNotSupported);
	}

	[Test]
	public async Task Generate_GivenGenericValueObject_ReportsGenericValueObjectsAreNotSupported(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Scalar]
				public readonly partial record struct EmailAddress<T>
				{
					public string Value { get; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.GenericValueObjectsAreNotSupported);
	}

	[Test]
	public async Task Generate_GivenMissingScalarProperty_ReportsScalarPropertyMissing(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Scalar]
				public readonly partial record struct EmailAddress
				{
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ScalarPropertyMissing);
	}

	[Test]
	public async Task Generate_GivenNonRecordStructScalar_ReportsScalarShouldBeRecordStruct(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Scalar]
				public partial struct EmailAddress
				{
					public string Value { get; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ScalarShouldBeRecordStruct);
	}

	[Test]
	public async Task Generate_GivenConflictingValueObjectAttributes_ReportsConflictingValueObjectAttributes(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Scalar]
				[ValueObject]
				public readonly partial record struct EmailAddress
				{
					public string Value { get; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ConflictingValueObjectAttributes);
	}

	[Test]
	public async Task Generate_GivenStrictModeWithoutCreate_ReportsStrictDeserializationRequiresCreate(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Scalar(DeserializationMode = ValueObjectDeserializationMode.Strict)]
				public readonly partial record struct EmailAddress
				{
					public string Value { get; }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.StrictDeserializationRequiresCreate);
	}

	[Test]
	public async Task Generate_GivenValidScalarValueObject_ReportsNoDiagnostics(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Scalar]
				public readonly partial record struct EmailAddress
				{
					public string Value { get; }
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
			options.WithAdditionalNamespaces(TypeLibrary.SerializationNamespace),
			cancellationToken
		);
	}
}
