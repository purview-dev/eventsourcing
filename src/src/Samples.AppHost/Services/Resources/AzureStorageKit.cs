using Aspire.Hosting.Azure;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.Services.Resources;

[ResourceDefinition<AzureStorageResource>]
sealed partial class AzureStorageKit
{
	public IResourceBuilder<AzureBlobStorageResource> Blobs { get; set; } = default!;

	protected override IResourceBuilder<AzureStorageResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var storage = builder
			.AddAzureStorage("storage")
			.RunAsEmulator(e =>
			{
				//if (!isTesting)
				//	e.WithDataVolume("eventsourcing-sample-azurite-data");

				e.WithImageTag(ContainerHelper.AzuriteImageTag);
			});

		Blobs = storage.AddBlobs(
			//isTesting ? $"ess-{Guid.NewGuid():N}"[..8] : "blob-storage"
			""
		);

		return storage;
	}
}
