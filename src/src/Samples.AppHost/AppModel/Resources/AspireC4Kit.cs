using Aspire.Hosting.AspireC4.ApplicationModel;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<AspireC4Resource>(Platform.AspireC4)]
sealed partial class AspireC4Kit
{
	protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder) =>
		!HostKit.Options.IsTestRun && !builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AspireC4Resource> BuildResource(
		IDistributedApplicationBuilder builder
	)
	{
		var aspireC4 = builder.AddAspireC4();
		return aspireC4;
	}
}
