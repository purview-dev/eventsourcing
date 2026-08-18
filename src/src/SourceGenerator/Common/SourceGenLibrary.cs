using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class SourceGenLibrary
{
	public static IncrementalValueProvider<AggregateGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context
	)
	{
		var generationContext =
			IncrementalPipeline.GenerationContextValueProvider<AggregateGenerationContext>(
				context,
				TypeLibrary.AggregateGeneratorName,
				AssemblyInfo.Version,
				(compilation, generatorSettings, logger, _) =>
					new(compilation, generatorSettings, logger),
				PropertyLibrary.DisableSourceGenerator
			);

		var aggregateInfo = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.Attributes.AggregateAttribute,
			predicate: static (s, _) => s is ClassDeclarationSyntax,
			transform: static (ctx, ct) => AggregateInfoBuilder.Build(ctx, ct),
			trackingName: "GetAggregateTargets"
		);

		return generationContext.CollectWith(
			aggregateInfo,
			static (context, aggregateInfo, _) =>
			{
				List<DiagnosticInfo> diagnostics = [];
				if (context.AggregateBase is null)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(DiagnosticLibrary.AggregateBaseReferenceMissing)
					);
				}

				AggregateGenerationModel model = new(context, aggregateInfo, new([.. diagnostics]));

				return model;
			},
			"CollectAggregates"
		);
	}
}
