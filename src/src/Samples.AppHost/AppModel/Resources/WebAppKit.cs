using System.ComponentModel.DataAnnotations;
using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<ProjectResource>(Platform.WebApp)]
sealed partial class WebAppKit
{
	protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var webApp = builder.AddProject<Projects.Samples_Web>(Name);

		return webApp;
	}

	//[ZodSchema]
	sealed partial class WebAppKitOptions
	{
		[Required]
		public Dictionary<string, ProjectConfiguration> Variants { get; set; } = [];

		[Required(AllowEmptyStrings = false)]
		public string AdminSitePath { get; set; } = string.Empty;

		[Required(AllowEmptyStrings = false)]
		public string AdminApiPath { get; set; } = string.Empty;

		[Required(AllowEmptyStrings = false)]
		public string DataIsolationWarning { get; set; } = string.Empty;
	}
}
