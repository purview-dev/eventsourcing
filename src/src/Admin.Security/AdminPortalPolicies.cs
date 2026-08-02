namespace Purview.EventSourcing.Admin.Security;

public static class AdminPortalPolicies
{
	public const string SearchAggregates = "AdminPortal.Aggregates.Search";
	public const string ViewAggregate = "AdminPortal.Aggregates.View";
	public const string ViewEvents = "AdminPortal.Events.View";
	public const string ProjectPointInTime = "AdminPortal.Projections.Execute";
	public const string ExportEvents = "AdminPortal.Events.Export";
}
