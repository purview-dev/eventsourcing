using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.EventSourcing.SourceGenerator.Generators;

namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class SourceGenLibrary
{
	public static IncrementalValueProvider<AggregateGenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context
	)
	{
		var generationContext = IncrementalPipeline.GenerationContextValueProvider<
			AggregateGenerationCapabilities,
			AggregateSourceGenerator
		>(
			context,
			static (compilation, settings, logger, _) =>
			{
				var hasAggregateBase =
					compilation.GetTypeByMetadataName(TypeLibrary.Aggregates.AggregateBase.MetadataFullName)
					is not null;
				return new(hasAggregateBase);
			},
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
				var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
				if (!context.Capabilities.HasAggregateBase)
					diagnostics.Add(DiagnosticInfo.Create(DiagnosticLibrary.AggregateBaseReferenceMissing));

				AggregateGenerationModel model = new(context, aggregateInfo, diagnostics.ToImmutable());

				return model;
			},
			"CollectAggregates"
		);
	}
}
