using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Extensions;

namespace Purview.EventSourcing.SourceGenerator.Generators;

[Generator]
public sealed partial class ValueObjectSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var scalarCandidates = context.SyntaxProvider.ForAttributeWithMetadataName(
			ValueObjectSymbolInspector.ScalarAttributeName,
			predicate: static (node, _) => node is TypeDeclarationSyntax,
			transform: static (ctx, ct) => BuildScalarGenerationResult(ctx, ct)
		);

		var complexCandidates = context.SyntaxProvider.ForAttributeWithMetadataName(
			ValueObjectSymbolInspector.ValueObjectAttributeName,
			predicate: static (node, _) => node is TypeDeclarationSyntax,
			transform: static (ctx, ct) => BuildComplexGenerationResult(ctx, ct)
		);

		context.RegisterSourceOutput(scalarCandidates, EmitResult);
		context.RegisterSourceOutput(complexCandidates, EmitResult);
	}

	static void EmitResult(SourceProductionContext context, ValueObjectGenerationResult result)
	{
		foreach (var diagnostic in result.Diagnostics)
			context.ReportDiagnostic(diagnostic);

		if (result.Source is not null && result.HintName is not null)
			context.AddSource(result.HintName, result.Source);
	}

	static ValueObjectGenerationResult BuildScalarGenerationResult(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var model = ScalarValueObjectModelBuilder.Build(
			context,
			cancellationToken,
			out var diagnostics
		);
		if (model is null)
			return new ValueObjectGenerationResult(null, null, diagnostics);

		var source = ScalarValueObjectEmitter.Emit(model);
		return new ValueObjectGenerationResult(model.HintName, source, diagnostics);
	}

	static ValueObjectGenerationResult BuildComplexGenerationResult(
		GeneratorAttributeSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var model = ComplexValueObjectModelBuilder.Build(
			context,
			cancellationToken,
			out var diagnostics
		);
		if (model is null)
			return new ValueObjectGenerationResult(null, null, diagnostics);

		var source = ComplexValueObjectEmitter.Emit(model);
		return new ValueObjectGenerationResult(model.HintName, source, diagnostics);
	}
}
