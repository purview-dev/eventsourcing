using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.Services.Resources;

[ResourceDefinition<ProjectResource>]
sealed partial class WebProjectKit
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Samples_Web>(Name).WithExternalHttpEndpoints();

	protected override void ConfigureResource()
	{
		ResourceBuilder
			.WithReference(HostKit.SqlServer.Database)
			.WaitFor(HostKit.SqlServer.Database)
			.WithReference(HostKit.AzureStorage.Blobs)
			.WaitFor(HostKit.AzureStorage.Blobs);

		if (HostKit.Redis.IsEnabled)
		{
			ResourceBuilder.WithReference(HostKit.Redis).WaitFor(HostKit.Redis);
		}
	}
}
