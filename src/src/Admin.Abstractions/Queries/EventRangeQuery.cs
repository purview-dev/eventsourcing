namespace Purview.EventSourcing.Admin.Abstractions;

public sealed record EventRangeQuery(
	long? VersionFrom,
	long? VersionTo,
	DateTimeOffset? TimeFromUtc,
	DateTimeOffset? TimeToUtc,
	int Page = 1,
	int PageSize = 50,
	string Sort = "Version asc"
);
