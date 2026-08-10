using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Purview.EventSourcing.SourceGenerator.Models;

namespace Purview.EventSourcing.SourceGenerator.Helpers;

static partial class SourceGenLibrary
{
	public static IncrementalValueProvider<AggregateGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabled = IncrementalPipeline.IsDisabledValueProvider(
			context,
			PropLibrary.DisableSourceGenerator
		);
		var generationContext = IncrementalPipeline.GenerationContextValueProvider(
			context,
			(compilation, _) => new AggregateGenerationContext(compilation),
			logger
		);

		var aggregates = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.Attributes.GenerateAggregateAttribute,
			predicate: (s, _) => s is ClassDeclarationSyntax,
			transform: (ctx, ct) =>
				GetAggregateInfo(ctx, logger, ct),
			trackingName: "GetAggregateTargets"
		);

		return isDisabled
			.CombineWith(
				generationContext,
				static (isDisabled, GenerationContext, _) =>
				{
					AggregateGenerationModel model = new(!isDisabled, GenerationContext);

					List<DiagnosticInfo> diagnostics = [];
					if (GenerationContext.AggregateBase is null)
					{
						diagnostics.Add(DiagnosticInfo.Create(GeneratorDiagnostics.AggregateBaseReferenceMissing, null));
					}

					if (diagnostics.Count > 0)
						model.Diagnostics = model.Diagnostics.AddRange(diagnostics);

					return model;
				},
				"CombineIsDisabledWithGenerationContext"
			)
			.CollectWith(
				aggregates,
				(model, aggregates, _) =>
				{
					model.Aggregates = aggregates;

					return model;
				},
				"CollectAggregates"
			);
	}


	public static string AddCodeGen(string source)
	{
		return source
			.Replace(
				CodeGenHelpers.CodeGenReplacementToken,
				CodeGenHelpers.GetGeneratedCodeAttribute()
			)
			.Replace(
				CodeGenHelpers.NonClassCodeGenReplacementToken,
				CodeGenHelpers.GetNonClassGeneratedCodeAttribute()
			);
	}

}
