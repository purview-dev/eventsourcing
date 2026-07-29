using System.ComponentModel.DataAnnotations;
using Aspire.Hosting.Azure;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<AzureStorageResource>(Platform.AzureStorage)]
sealed partial class AzureStorageKit
{
	public IResourceBuilder<AzureBlobStorageContainerResource> SnapshotBlob { get; set; } = default!;

	protected override IResourceBuilder<AzureStorageResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var storage = builder.AddAzureStorage(Name);
		if (!builder.ExecutionContext.IsPublishMode)
		{
			storage.RunAsEmulator(e =>
			{
				if (!HostKit.Options.IsTestRun)
					e.WithDataVolume();

				e.WithImageTag(ContainerHelper.AzuriteImageTag);
			});
		}

		SnapshotBlob = storage.AddBlobContainer(Platform.AzureStorageBlob, Options.BlobName);

		return storage;
	}

	partial class AzureStorageKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		public string BlobName { get; set; } = "blob";
	}
}
