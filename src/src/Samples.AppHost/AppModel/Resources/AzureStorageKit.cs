using System.ComponentModel.DataAnnotations;
using Aspire.Hosting.Azure;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<AzureStorageResource>(Platform.AzureStorage)]
sealed partial class AzureStorageKit
{
	public IResourceBuilder<AzureStorageResource> Storage { get; private set; }

	public IResourceBuilder<AzureTableStorageResource> TableStorage { get; private set; }

	public IResourceBuilder<AzureBlobStorageContainerResource> SnapshotBlob { get; set; }

	protected override IResourceBuilder<AzureStorageResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		Storage = builder.AddAzureStorage(Name);
		var storage = Storage;
		if (!builder.ExecutionContext.IsPublishMode)
		{
			storage.RunAsEmulator(e =>
			{
				if (HostKit.Options.UseDataVolumes)
					e.WithDataVolume();

				e.WithImageTag(ContainerHelper.AzuriteImageTag);
			});
		}

		TableStorage = storage.AddTables(Platform.AzureStorageTable);
		SnapshotBlob = storage.AddBlobContainer(Platform.AzureStorageBlob, Options.BlobName);

		return storage;
	}

	partial class AzureStorageKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		public string BlobName { get; set; } = "blob";
	}
}
