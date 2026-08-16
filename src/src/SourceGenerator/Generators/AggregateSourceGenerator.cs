using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Extensions;

namespace Purview.EventSourcing.SourceGenerator;

[Generator]
public sealed partial class AggregateSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx =>
		{
			_logger?.Debug("Adding attribute definitions to compilation");

			ctx.AddEmbeddedAttributeDefinition();
			_logger?.Debug($" - EmbeddedAttribute");

			foreach (var (HintName, Source) in AggregateAttributeEmitter.Emit())
			{
				_logger?.Debug($" - {HintName}");
				ctx.AddSource(HintName, Source);
			}
		});

		var generationModel = SourceGenLibrary.GetGeneratorValueProviders(context, _logger);

		context.RegisterSourceOutput(
			generationModel,
			(spc, model) =>
			{
				if (!model.IsSourceGeneratorEnabled)
				{
					_logger?.Info("Aggregate source generator is disabled.");
					return;
				}

				spc.ReportDiagnostics(model.Diagnostics);

				foreach (var aggregateResult in model.Aggregates)
				{
					spc.ReportDiagnostics(aggregateResult.Diagnostics);

					if (aggregateResult.IsFatal || aggregateResult.Value is null)
						continue;

					var info = aggregateResult.Value;
					var writer = model.GenerationContext.CreateCodeWriter();
					AggregateSourceEmitter.GenerateAggregateSource(writer, info, _logger);
					spc.AddSource(info.HintName, writer);
				}
			}
		);

		SourceGenLibrary.RegisterAdditionalDiagnostics(context);
	}
}
