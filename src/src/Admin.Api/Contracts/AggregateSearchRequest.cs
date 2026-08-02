namespace Purview.EventSourcing.Admin.Api.Contracts;

public sealed record AggregateSearchRequest(
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
