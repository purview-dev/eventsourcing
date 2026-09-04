using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.EventSourcing.SourceGenerator.Generators;

namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class SourceGenLibrary
{
	public static GenerationSettings CreateGenerationSettings<TGenerator>(string? disablePropertyName = null) =>
		GenerationSettings.Create<TGenerator>(disablePropertyName) with
		{
			DefaultMethodAccessibility = null,
		};

	public static IncrementalValueProvider<GenerationContext<AggregateGenerationCapabilities>> GetGenerationContext(
		IncrementalGeneratorInitializationContext context
	) =>
		IncrementalPipeline.GenerationContextValueProvider<AggregateGenerationCapabilities>(
			context,
			CreateGenerationSettings<AggregateSourceGenerator>(PropertyLibrary.DisableSourceGenerator),
			static (compilation, settings, logger, _) =>
			{
				var hasAggregateBase =
					compilation.GetTypeByMetadataName(TypeLibrary.Aggregates.AggregateBase.MetadataFullName)
					is not null;
				return new(hasAggregateBase);
			}
		);

	public static IncrementalValuesProvider<GeneratorResult<AggregateInfo>> GetAggregateTargets(
		IncrementalGeneratorInitializationContext context
	) =>
		IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.Attributes.AggregateAttribute,
			predicate: static (s, _) => s is ClassDeclarationSyntax,
			transform: static (ctx, ct) =>
				AggregateInfoBuilder.Build(
					(INamedTypeSymbol)ctx.TargetSymbol,
					(ClassDeclarationSyntax)ctx.TargetNode,
					ctx.SemanticModel.Compilation,
					ct
				),
			trackingName: "GetAggregateTargets"
		);
}
