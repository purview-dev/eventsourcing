namespace Purview.EventSourcing.Admin.Api;

public sealed record EventRangeRequest(
	long? VersionFrom = null,
	long? VersionTo = null,
	DateTimeOffset? TimeFromUtc = null,
	DateTimeOffset? TimeToUtc = null,
	int Page = 1,
	int PageSize = 50,
	string Sort = "Version asc");
