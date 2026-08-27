using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Purview.EventSourcing.SourceGenerator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventStoreAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[
			DiagnosticLibrary.ComputedParameterCannotBeSetByCaller,
			DiagnosticLibrary.EventNameShouldBePastTense,
			DiagnosticLibrary.NullableScalarEqualityNullComparisonShouldUsePatternMatching,
			DiagnosticLibrary.EventMethodRequiresAggregateAttribute,
		];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		RegisterComputedParameterDiagnostics(context);
		RegisterManualEventTypeDiagnostics(context);
		RegisterNullableScalarComparisonDiagnostics(context);
		RegisterOrphanEventMethodDiagnostics(context);
	}

	static void RegisterComputedParameterDiagnostics(AnalysisContext context) =>
		context.RegisterOperationAction(
			static context =>
			{
				var invocation = (IInvocationOperation)context.Operation;
				var targetMethod = invocation.TargetMethod;

				if (
					!TypeHelpers.HasAttribute(targetMethod, TypeLibrary.Attributes.EventAttribute)
					&& !TypeHelpers.HasAttribute(targetMethod, TypeLibrary.Attributes.CollectionEventAttribute)
				)
				{
					return;
				}

				var computedParameter = targetMethod.Parameters.FirstOrDefault(static parameter =>
					TypeHelpers.HasAttribute(parameter, TypeLibrary.Attributes.ComputedAttribute)
				);

				if (computedParameter is null)
					return;

				var passedArgument = invocation.Arguments.FirstOrDefault(argument =>
					!argument.IsImplicit && SymbolEqualityComparer.Default.Equals(argument.Parameter, computedParameter)
				);

				if (passedArgument is null)
					return;

				context.ReportDiagnostic(
					Diagnostic.Create(
						DiagnosticLibrary.ComputedParameterCannotBeSetByCaller,
						passedArgument.Syntax.GetLocation(),
						targetMethod.Name,
						computedParameter.Name
					)
				);
			},
			OperationKind.Invocation
		);

	static void RegisterNullableScalarComparisonDiagnostics(AnalysisContext context)
	{
		context.RegisterSyntaxNodeAction(
			static context =>
			{
				var binaryExpression = (BinaryExpressionSyntax)context.Node;

				ExpressionSyntax? comparedExpression = null;

				if (binaryExpression.Left.IsKind(SyntaxKind.NullLiteralExpression))
				{
					comparedExpression = binaryExpression.Right;
				}
				else if (binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression))
				{
					comparedExpression = binaryExpression.Left;
				}

				if (comparedExpression is null)
					return;

				var comparedType = context
					.SemanticModel.GetTypeInfo(comparedExpression, context.CancellationToken)
					.Type;

				if (comparedType is null || !IsScalarValueObject(comparedType))
					return;

				var replacement = binaryExpression.IsKind(SyntaxKind.EqualsExpression) ? "is null" : "is not null";

				context.ReportDiagnostic(
					Diagnostic.Create(
						DiagnosticLibrary.NullableScalarEqualityNullComparisonShouldUsePatternMatching,
						binaryExpression.GetLocation(),
						binaryExpression.ToString(),
						replacement
					)
				);
			},
			SyntaxKind.EqualsExpression,
			SyntaxKind.NotEqualsExpression
		);
	}

	static void RegisterManualEventTypeDiagnostics(AnalysisContext context)
	{
		context.RegisterSymbolAction(
			static context =>
			{
				var typeSymbol = (INamedTypeSymbol)context.Symbol;

				if (!TypeHelpers.InheritsFrom(typeSymbol, TypeLibrary.Aggregates.EventBase))
					return;

				if (TypeHelpers.HasAttribute(typeSymbol, TypeLibrary.Attributes.SentinelEventAttribute))
				{
					return;
				}

				if (EventVerbMap.IsPastTenseEventName(typeSymbol.Name))
					return;

				var location = typeSymbol.Locations.FirstOrDefault(static location => location.IsInSource);

				if (location is null)
					return;

				context.ReportDiagnostic(
					Diagnostic.Create(DiagnosticLibrary.EventNameShouldBePastTense, location, typeSymbol.Name)
				);
			},
			SymbolKind.NamedType
		);
	}

	static void RegisterOrphanEventMethodDiagnostics(AnalysisContext context)
	{
		context.RegisterSymbolAction(
			static context =>
			{
				var methodSymbol = (IMethodSymbol)context.Symbol;

				if (
					!TypeHelpers.HasAttribute(methodSymbol, TypeLibrary.Attributes.EventAttribute)
					&& !TypeHelpers.HasAttribute(methodSymbol, TypeLibrary.Attributes.CollectionEventAttribute)
				)
				{
					return;
				}

				if (TypeHelpers.HasAttribute(methodSymbol.ContainingType, TypeLibrary.Attributes.AggregateAttribute))
					return;

				var location = methodSymbol.Locations.FirstOrDefault(static location => location.IsInSource);
				if (location is null)
					return;

				context.ReportDiagnostic(
					Diagnostic.Create(
						DiagnosticLibrary.EventMethodRequiresAggregateAttribute,
						location,
						methodSymbol.Name
					)
				);
			},
			SymbolKind.Method
		);
	}

	static bool IsScalarValueObject(ITypeSymbol type)
	{
		if (
			type is INamedTypeSymbol namedType
			&& namedType.IsGenericType
			&& namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
		)
		{
			type = namedType.TypeArguments[0];
		}

		return TypeHelpers.HasAttribute(type, TypeLibrary.Attributes.ScalarAttribute);
	}
}
