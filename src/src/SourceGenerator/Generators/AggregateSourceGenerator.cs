namespace Purview.EventSourcing.SourceGenerator.Generators;

[Generator]
public sealed partial class AggregateSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterEmbeddedAttribute(typeof(AggregateSourceGenerator).FullName, AssemblyInfo.Version);
		context.RegisterPostInitializationOutput(ctx =>
		{
			foreach (var (HintName, Source) in AggregateAttributeEmitter.Emit())
				ctx.AddSource(HintName, Source);
		});

		var generationModel = SourceGenLibrary.GetGeneratorValueProviders(context);

		context.RegisterSourceOutput(
			generationModel,
			(spc, model) =>
			{
				if (model.GenerationContext.Settings.IsSourceGeneratorDisabled)
					return;

				spc.ReportDiagnostics(model.Diagnostics);

				foreach (var aggregateResult in model.Aggregates)
				{
					if (aggregateResult.HasDiagnostics)
						spc.ReportDiagnostics(aggregateResult.Diagnostics);

					if (aggregateResult.IsFatal || aggregateResult.Value is null)
						continue;

					var info = aggregateResult.Value;

					AggregateEmitContext outputContext = new(model.GenerationContext, info);

					AggregateSourceEmitter.GenerateAggregateSource(outputContext);

					spc.AddSource(info.HintName, outputContext.Writer);
				}
			}
		);

		SourceGenLibrary.RegisterAdditionalDiagnostics(context);
	}
}
