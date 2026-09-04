using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.EventSourcing.SourceGenerator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueObjectDiagnosticAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[
			DiagnosticLibrary.ValueObjectMustBePartial,
			DiagnosticLibrary.NestedValueObjectsAreNotSupported,
			DiagnosticLibrary.GenericValueObjectsAreNotSupported,
			DiagnosticLibrary.ConflictingValueObjectAttributes,
			DiagnosticLibrary.ScalarPropertyMissing,
			DiagnosticLibrary.ScalarShouldBeRecordStruct,
			DiagnosticLibrary.StrictDeserializationRequiresCreate,
		];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSymbolAction(ValidateValueObject, SymbolKind.NamedType);
	}

	static void ValidateValueObject(SymbolAnalysisContext context)
	{
		var typeSymbol = (INamedTypeSymbol)context.Symbol;

		var hasScalarAttribute = TypeHelpers.HasAttribute(typeSymbol, ValueObjectSymbolInspector.ScalarAttributeName);
		var hasValueObjectAttribute = TypeHelpers.HasAttribute(
			typeSymbol,
			ValueObjectSymbolInspector.ValueObjectAttributeName
		);
		if (!hasScalarAttribute && !hasValueObjectAttribute)
			return;

		if (
			typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken)
			is not TypeDeclarationSyntax syntax
		)
			return;

		if (hasScalarAttribute)
		{
			var result = ScalarValueObjectModelBuilder.Build(typeSymbol, syntax, context.CancellationToken);

			foreach (var diagnostic in result.Diagnostics)
				context.ReportDiagnostic(diagnostic.ToDiagnostic());
		}
		else
		{
			var result = ComplexValueObjectModelBuilder.Build(
				typeSymbol,
				syntax,
				context.Compilation,
				context.CancellationToken
			);

			foreach (var diagnostic in result.Diagnostics)
				context.ReportDiagnostic(diagnostic.ToDiagnostic());
		}
	}
}
