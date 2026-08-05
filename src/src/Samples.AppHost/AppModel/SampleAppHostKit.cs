using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel;

[HostKit]
sealed partial class SampleAppHostKit
{
	//[ZodSchema]
	partial class SampleAppHostKitOptions
	{
		public bool IsTestRun { get; set; }

		public bool IsLocal { get; set; }

		public bool UseDataVolumes { get; set; }
	}
}
