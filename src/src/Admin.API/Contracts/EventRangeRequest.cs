namespace Purview.EventSourcing.Admin.Api.Contracts;

/// <summary>
/// The request contract for reading a range of events from an aggregate stream.
/// </summary>
/// <param name="VersionFrom">The inclusive lower bound of the stream version to query, or <see langword="null"/> to start from the beginning.</param>
/// <param name="VersionTo">The inclusive upper bound of the stream version to query, or <see langword="null"/> to include the latest events.</param>
/// <param name="TimeFromUtc">The inclusive lower bound of the event timestamp (UTC) to query, or <see langword="null"/> to ignore the time bound.</param>
/// <param name="TimeToUtc">The inclusive upper bound of the event timestamp (UTC) to query, or <see langword="null"/> to ignore the time bound.</param>
/// <param name="Page">The one-based page number to return.</param>
/// <param name="PageSize">The maximum number of events to return per page.</param>
/// <param name="Sort">The sort expression, for example <c>"Version asc"</c>.</param>
public sealed record EventRangeRequest(
	long? VersionFrom = null,
	long? VersionTo = null,
	DateTimeOffset? TimeFromUtc = null,
	DateTimeOffset? TimeToUtc = null,
	int Page = 1,
	int PageSize = 50,
	string Sort = "Version asc"
);
