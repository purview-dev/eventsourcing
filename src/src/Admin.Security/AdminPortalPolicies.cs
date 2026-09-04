namespace Purview.EventSourcing.Admin.Security;

/// <summary>
/// Defines the authorization policy names used by the admin portal.
/// </summary>
public static class AdminPortalPolicies
{
	/// <summary>
	/// Policy that authorizes searching for aggregates.
	/// </summary>
	public const string SearchAggregates = "AdminPortal.Aggregates.Search";

	/// <summary>
	/// Policy that authorizes viewing a single aggregate.
	/// </summary>
	public const string ViewAggregate = "AdminPortal.Aggregates.View";

	/// <summary>
	/// Policy that authorizes viewing an aggregate's event history.
	/// </summary>
	public const string ViewEvents = "AdminPortal.Events.View";

	/// <summary>
	/// Policy that authorizes projecting aggregate state at a point in time.
	/// </summary>
	public const string ProjectPointInTime = "AdminPortal.Projections.Execute";

	/// <summary>
	/// Policy that authorizes exporting events from an aggregate stream.
	/// </summary>
	public const string ExportEvents = "AdminPortal.Events.Export";
}
