using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.Generators;

[Generator]
public sealed partial class ValueObjectSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context
			.RegisterEmbeddedAttribute<ValueObjectSourceGenerator>()
			.RegisterPostInitializationOutput(ctx =>
			{
				foreach (var (HintName, Source) in ValueObjectsAttributeEmitter.EmitAttributes())
					ctx.AddSource(HintName, Source);
			});

		var generationContext = IncrementalPipeline.GenerationContextValueProvider<
			EmptyCapabilities,
			ValueObjectSourceGenerator
		>(context, static (_, _, _, _) => EmptyCapabilities.Instance, PropertyLibrary.DisableSourceGenerator);

		var scalarCandidates = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				ValueObjectSymbolInspector.ScalarAttributeName,
				predicate: static (node, _) => node is TypeDeclarationSyntax,
				transform: static (ctx, ct) =>
					ScalarValueObjectModelBuilder.Build(
						(INamedTypeSymbol)ctx.TargetSymbol,
						(TypeDeclarationSyntax)ctx.TargetNode,
						ct
					)
			)
			.WithTrackingName("GetScalarValueObjectTargets");

		var complexCandidates = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				ValueObjectSymbolInspector.ValueObjectAttributeName,
				predicate: static (node, _) => node is TypeDeclarationSyntax,
				transform: static (ctx, ct) =>
					ComplexValueObjectModelBuilder.Build(
						(INamedTypeSymbol)ctx.TargetSymbol,
						(TypeDeclarationSyntax)ctx.TargetNode,
						ctx.SemanticModel.Compilation,
						ct
					)
			)
			.WithTrackingName("GetComplexValueObjectTargets");

		context.RegisterSourceOutput(
			scalarCandidates.Combine(generationContext),
			static (spc, tuple) => EmitScalarResult(spc, tuple.Left, tuple.Right)
		);
		context.RegisterSourceOutput(
			complexCandidates.Combine(generationContext),
			static (spc, tuple) => EmitComplexResult(spc, tuple.Left, tuple.Right)
		);
	}

	static void EmitScalarResult(
		SourceProductionContext context,
		GeneratorResult<ScalarValueObjectModel> result,
		GeneratorContext generationContext
	)
	{
		if (generationContext.Settings.IsSourceGeneratorDisabled)
			return;

		if (!result.ShouldProcess)
			return;

		var writer = generationContext.CreateCodeWriter();
		ScalarValueObjectEmitter.Emit(writer, result.Value);
		context.AddSource(result.Value.HintName, writer);
	}

	static void EmitComplexResult(
		SourceProductionContext context,
		GeneratorResult<ComplexValueObjectModel> result,
		GeneratorContext generationContext
	)
	{
		if (generationContext.Settings.IsSourceGeneratorDisabled)
			return;

		if (!result.ShouldProcess)
			return;

		var writer = generationContext.CreateCodeWriter();
		ComplexValueObjectEmitter.Emit(writer, result.Value);
		context.AddSource(result.Value.HintName, writer);
	}
}
