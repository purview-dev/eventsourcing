using System.ComponentModel.DataAnnotations;
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
				if (!HostKit.Options.IsTestRun)
					e.WithDataVolume();

				e.WithImageTag(ContainerHelper.AzuriteImageTag);
			});

		Blobs = storage.AddBlobs(
			//isTesting ? $"ess-{Guid.NewGuid():N}"[..8] : "blob-storage"
			Options.BlobName
		);

		return storage;
	}

	partial class AzureStorageKitOptions
	{
		[Required(AllowEmptyStrings = false)]
		public string BlobName { get; set; } = "blob";
	}
}
