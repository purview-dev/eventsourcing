namespace Purview.EventSourcing.Admin.Abstractions;

public sealed record AggregateSearchQuery(
	string? AggregateType,
	string? AggregateId,
	DateTimeOffset? FromUtc,
	DateTimeOffset? ToUtc,
	bool? IsDeleted,
	bool? IsRestored,
	int Page = 1,
	int PageSize = 50,
	string Sort = "LastUpdatedUtc desc"
);
