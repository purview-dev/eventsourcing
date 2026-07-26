using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.Services;

[HostApp]
sealed partial class KitApp;

partial class KitAppOptions
{
	public bool IsTestRun { get; set; }
}
