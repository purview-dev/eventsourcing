using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class SourceGenLibrary
{
	public static IncrementalValueProvider<AggregateGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabled = IncrementalPipeline.IsDisabledValueProvider(
			context,
			PropertyLibrary.DisableSourceGenerator
		);
		var generationContext = IncrementalPipeline.GenerationContextValueProvider(
			context,
			generatorName: TypeLibrary.AggregateGeneratorName,
			generatorVersion: AssemblyInfo.Version,
			(compilation, validateCodeWriterScopes, generatorName, generatorVersion, _) =>
				new AggregateGenerationContext(
					compilation,
					generatorName,
					generatorVersion,
					validateCodeWriterScopes
				),
			logger
		);

		var aggregates = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.Attributes.AggregateAttribute,
			predicate: (s, _) => s is ClassDeclarationSyntax,
			transform: (ctx, ct) => AggregateInfoBuilder.Build(ctx, logger, ct),
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
						diagnostics.Add(
							DiagnosticInfo.Create(
								GeneratorDiagnostics.AggregateBaseReferenceMissing
							)
						);

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
}
