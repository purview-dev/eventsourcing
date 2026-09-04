using System.ComponentModel.DataAnnotations;
using Purview.EventSourcing.Samples.Options;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

partial class WebAppKit
{
	public sealed class VariantConfiguration
	{
		[Required(AllowEmptyStrings = false)]
		public string ResourceName { get; set; } = string.Empty;

		[Required(AllowEmptyStrings = false)]
		public string DisplayName { get; set; } = string.Empty;

		[Required(AllowEmptyStrings = false)]
		public string Description { get; set; } = string.Empty;

		public SampleEventStoreKind EventStore { get; set; }

		public SampleQueryStoreKind QueryStore { get; set; }

		public SampleAdminStoreKind AdminStore { get; set; }

		public bool AdminAPIAvailable { get; set; } = true;

		[Required(AllowEmptyStrings = false)]
		public string EventStoreConnectionName { get; set; } = string.Empty;

		[Required(AllowEmptyStrings = false)]
		public string QueryStoreConnectionName { get; set; } = string.Empty;

		[RegularExpression(@"^[\w\-.]+$")]
		public string? EventStoreDatabaseName { get; set; }

		[RegularExpression(@"^[\w\-.]+$")]
		public string? QueryStoreDatabaseName { get; set; }

		[RegularExpression(@"^[\w\-.]+$")]
		public string? AdminDatabaseName { get; set; }
	}
}
