using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
[DependsOn<BuildModule>]
public class RunTestsModule(IOptions<BuildSettings> settings) : Module<CommandResult[]>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				settings.Value.RunTests
					? SkipDecision.DoNotSkip
					: SkipDecision.Skip("Tests are disabled. Set Build__RunTests=true to run them.")
			)
			.Build();

	protected override async Task<CommandResult[]?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var testProjects = Directory.EnumerateFiles("src/tests", "*Tests.csproj", SearchOption.AllDirectories).ToList();
		if (testProjects.Count == 0)
		{
			context.Logger.LogWarning(
				"No test projects found in 'src/tests', despite tests being enabled. Skipping test execution."
			);

			return [];
		}

		var tasks = testProjects.Select(project =>
			context
				.DotNet()
				.Test(
					new DotNetTestOptions
					{
						Project = project,
						Configuration = settings.Value.Configuration,
						NoBuild = true,
						NoRestore = true,
						Arguments = ["--ignore-exit-code", "8", "--treenode-filter", settings.Value.TestFilter],
					},
					cancellationToken: cancellationToken
				)
		);

		return await Task.WhenAll(tasks);
	}
}
