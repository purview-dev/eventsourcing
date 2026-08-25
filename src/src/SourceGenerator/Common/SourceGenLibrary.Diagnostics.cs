#pragma warning disable IDE0370 // Suppression operator is required to change the element type of the incremental provider

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class SourceGenLibrary
{
	public static void RegisterAdditionalDiagnostics(IncrementalGeneratorInitializationContext context)
	{
		RegisterOrphanEventMethodDiagnostics(context);
		RegisterManualEventTypeDiagnostics(context);
		RegisterNullableScalarComparisonDiagnostics(context);
		RegisterComputedParameterInvocationDiagnostics(context);
	}

	static void RegisterOrphanEventMethodDiagnostics(IncrementalGeneratorInitializationContext context)
	{
		var eventDiagnostics = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				TypeLibrary.Attributes.EventAttribute.MetadataFullName,
				static (node, _) => node is MethodDeclarationSyntax,
				static (ctx, ct) => CreateOrphanEventMethodDiagnostic(ctx.TargetSymbol as IMethodSymbol)
			)
			.Where(static d => d is not null)
			.Select(static (d, _) => d!)
			.Collect();

		var collectionEventDiagnostics = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				TypeLibrary.Attributes.CollectionEventAttribute.MetadataFullName,
				static (node, _) => node is MethodDeclarationSyntax,
				static (ctx, ct) => CreateOrphanEventMethodDiagnostic(ctx.TargetSymbol as IMethodSymbol)
			)
			.Where(static d => d is not null)
			.Select(static (d, _) => d!)
			.Collect();

		var all = eventDiagnostics
			.Combine(collectionEventDiagnostics)
			.Select(static (pair, _) => pair.Left.AddRange(pair.Right));

		context.RegisterSourceOutput(all, static (spc, diagnostics) => spc.ReportDiagnostics(diagnostics));
	}

	static DiagnosticInfo? CreateOrphanEventMethodDiagnostic(IMethodSymbol? method)
	{
		if (method is null)
			return null;

		if (TypeHelpers.HasAttribute(method.ContainingType, TypeLibrary.Attributes.AggregateAttribute))
		{
			return null;
		}

		// If the method is not contained within an aggregate, report a diagnostic
		return DiagnosticInfo.Create(DiagnosticLibrary.EventMethodRequiresAggregateAttribute, method, method.Name);
	}

	static void RegisterManualEventTypeDiagnostics(IncrementalGeneratorInitializationContext context)
	{
		var diagnostics = context
			.SyntaxProvider.CreateSyntaxProvider(
				static (node, _) => node is TypeDeclarationSyntax,
				static (ctx, ct) =>
				{
					if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) is not INamedTypeSymbol typeSymbol)
						return null;

					if (!TypeHelpers.InheritsFrom(typeSymbol, TypeLibrary.Aggregates.EventBase))
						return null;

					var location = typeSymbol.Locations.FirstOrDefault();
					if (location?.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) == true)
						return null;

					if (TypeHelpers.HasAttribute(typeSymbol, TypeLibrary.Attributes.SentinelEventAttribute))
					{
						return null;
					}

					if (EventVerbMap.IsPastTenseEventName(typeSymbol.Name))
						return null;

					// If the event name is not in past tense, report a diagnostic
					return DiagnosticInfo.Create(
						DiagnosticLibrary.EventNameShouldBePastTense,
						typeSymbol,
						typeSymbol.Name
					);
				}
			)
			.Where(static d => d is not null)
			.Select(static (d, _) => d!)
			.Collect();

		context.RegisterSourceOutput(diagnostics, static (spc, diagnostics) => spc.ReportDiagnostics(diagnostics));
	}

	static void RegisterNullableScalarComparisonDiagnostics(IncrementalGeneratorInitializationContext context)
	{
		var diagnostics = context
			.SyntaxProvider.CreateSyntaxProvider(
				static (node, _) =>
					node is BinaryExpressionSyntax be
					&& (
						be.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken)
						|| be.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken)
					),
				static (ctx, ct) =>
				{
					var be = (BinaryExpressionSyntax)ctx.Node;

					ExpressionSyntax? otherExpression = null;
					if (be.Left.IsKind(SyntaxKind.NullLiteralExpression))
						otherExpression = be.Right;
					else if (be.Right.IsKind(SyntaxKind.NullLiteralExpression))
						otherExpression = be.Left;

					if (otherExpression is null)
						return null;

					var otherType = ctx.SemanticModel.GetTypeInfo(otherExpression, ct).Type;
					if (otherType is null)
						return null;

					if (!IsScalarValueObject(otherType))
						return null;

					var replacement = be.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken) ? "is null" : "is not null";

					return DiagnosticInfo.Create(
						DiagnosticLibrary.NullableScalarEqualityNullComparisonShouldUsePatternMatching,
						be.GetLocation(),
						be.ToString(),
						replacement
					);
				}
			)
			.Where(static d => d is not null)
			.Select(static (d, _) => d!)
			.Collect();

		context.RegisterSourceOutput(diagnostics, static (spc, diagnostics) => spc.ReportDiagnostics(diagnostics));
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

	static void RegisterComputedParameterInvocationDiagnostics(IncrementalGeneratorInitializationContext context)
	{
		var diagnostics = context
			.SyntaxProvider.CreateSyntaxProvider(
				static (node, _) => node is InvocationExpressionSyntax,
				static (ctx, ct) =>
				{
					var invocation = (InvocationExpressionSyntax)ctx.Node;
					var operation = ctx.SemanticModel.GetOperation(invocation, ct) as IInvocationOperation;
					if (operation?.TargetMethod is not IMethodSymbol targetMethod)
						return null;

					if (
						!TypeHelpers.HasAttribute(targetMethod, TypeLibrary.Attributes.EventAttribute)
						&& !TypeHelpers.HasAttribute(targetMethod, TypeLibrary.Attributes.CollectionEventAttribute)
					)
						return null;

					var computedParameter = targetMethod.Parameters.FirstOrDefault(p =>
						TypeHelpers.HasAttribute(p, TypeLibrary.Attributes.ComputedAttribute)
					);
					if (computedParameter is null)
						return null;

					var passedArgument = operation.Arguments.FirstOrDefault(a =>
						!a.IsImplicit && SymbolEqualityComparer.Default.Equals(a.Parameter, computedParameter)
					);
					if (passedArgument is null)
						return null;

					// If the computed parameter is being set by the caller, report a diagnostic
					return DiagnosticInfo.Create(
						DiagnosticLibrary.ComputedParameterCannotBeSetByCaller,
						targetMethod,
						targetMethod.Name,
						computedParameter.Name
					);
				}
			)
			.Where(static d => d is not null)
			.Select(static (d, _) => d!)
			.Collect();

		context.RegisterSourceOutput(diagnostics, static (spc, diagnostics) => spc.ReportDiagnostics(diagnostics));
	}
}

#pragma warning restore IDE0370
