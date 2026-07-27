using Aspire.Hosting.AspireC4.ApplicationModel;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.Services.Resources;

[ResourceDefinition<AspireC4Resource>]
sealed partial class AspireC4Kit
{
	protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder) =>
		!builder.ExecutionContext.IsPublishMode;

	protected override IResourceBuilder<AspireC4Resource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var aspireC4 = builder.AddAspireC4();
		return aspireC4;
	}
}
