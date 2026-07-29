using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<ProjectResource>(Platform.WebApp)]
sealed partial class WebProjectKit
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Samples_Web>(Name).WithExternalHttpEndpoints();

	protected override void ConfigureResource()
	{
		ResourceBuilder.WithReference(HostKit.SqlServer.Database).WaitFor(HostKit.SqlServer.Database);

		if (HostKit.AzureStorage.IsEnabled)
		{
			ResourceBuilder.WithReference(HostKit.AzureStorage.SnapshotBlob).WaitFor(HostKit.AzureStorage.SnapshotBlob);
		}

		if (HostKit.Redis.IsEnabled)
		{
			ResourceBuilder.WithReference(HostKit.Redis).WaitFor(HostKit.Redis);
		}
	}
}
