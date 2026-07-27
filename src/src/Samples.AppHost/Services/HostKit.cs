using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.Services;

[HostKit]
sealed partial class HostKit
{
	partial class HostKitOptions
	{
		public bool IsTestRun { get; set; }
	}
}
