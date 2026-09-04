namespace Purview.EventSourcing.SourceGenerator.Generators;

[Generator]
public sealed partial class AggregateSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context
			.RegisterEmbeddedAttribute<AggregateSourceGenerator>()
			.RegisterPostInitializationOutput(ctx =>
			{
				foreach (var (HintName, Source) in AggregateAttributeEmitter.Emit())
					ctx.AddSource(HintName, Source);
			});

		var generationContext = SourceGenLibrary.GetGenerationContext(context);
		var aggregateInfo = SourceGenLibrary.GetAggregateTargets(context);

		context.RegisterSourceOutput(
			aggregateInfo.Combine(generationContext),
			static (spc, tuple) =>
			{
				var (aggregateResult, context) = tuple;
				if (context.Settings.IsSourceGeneratorDisabled)
					return;

				if (!aggregateResult.ShouldProcess || !aggregateResult.HasValue)
					return;

				var info = aggregateResult.Value;
				var writer = context.CreateCodeWriter();

				AggregateEmitContext outputContext = new(context, info);
				AggregateSourceEmitter.GenerateAggregateSource(outputContext, writer);

				spc.AddSource(info.HintName, writer);
			}
		);
	}
}
