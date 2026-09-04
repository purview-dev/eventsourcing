using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
public sealed class LintModule : Module<CommandResult>
{
	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var dotnet = context.DotNet();
		await dotnet.Tool.Restore(new() { Interactive = false }, new(), cancellationToken);

		var pipelineDirectory = PipelineProjectDirectory.Find();
		var repositoryRoot = PathHelpers.FindRepositoryRoot(pipelineDirectory);

		return await context.Shell.Command.ExecuteCommandLineTool(
			DotNetCLIOptions.Create("tool", "run", "csharpier", "check", repositoryRoot),
			cancellationToken: cancellationToken
		);
	}
}
