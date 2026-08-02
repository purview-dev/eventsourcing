namespace Purview.EventSourcing.Admin.Abstractions.Models;

public sealed record AggregateSummaryResponse(
	string AggregateType,
	string AggregateId,
	long CurrentVersion,
	DateTimeOffset CreatedUtc,
	DateTimeOffset LastUpdatedUtc,
	bool IsDeleted,
	bool IsRestored
);
