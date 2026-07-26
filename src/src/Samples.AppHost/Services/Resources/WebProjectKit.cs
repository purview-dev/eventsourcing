using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.Services.Resources;

[ResourceDefinition<ProjectResource>]
sealed partial class WebProjectKit
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder) =>
		builder.AddProject<Projects.Samples_Web>(Name).WithExternalHttpEndpoints();

	protected override void ConfigureResource(KitApp app)
	{
		ResourceBuilder
			.WithReference(app.SqlServer.Database)
			.WaitFor(app.SqlServer.Database)
			.WithReference(app.AzureStorage.Blobs)
			.WaitFor(app.AzureStorage.Blobs);

		if (app.Redis.IsEnabled)
		{
			ResourceBuilder.WithReference(app.Redis.ResourceBuilder).WaitFor(app.Redis.ResourceBuilder);
		}
	}
}
