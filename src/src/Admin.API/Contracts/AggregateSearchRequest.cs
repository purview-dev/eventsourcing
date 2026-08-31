using System.ComponentModel.DataAnnotations;
using ZodSharp;

namespace Purview.EventSourcing.Admin.Api.Contracts;

/// <summary>
/// The request contract for searching aggregates in the admin portal.
/// </summary>
/// <param name="AggregateType">The aggregate type name to filter by, or <see langword="null"/> to match all types.</param>
/// <param name="AggregateId">The aggregate identifier to filter by, or <see langword="null"/> to match all identifiers.</param>
/// <param name="FromUtc">The inclusive lower bound of the last-updated timestamp (UTC) to filter by, or <see langword="null"/> to ignore the bound.</param>
/// <param name="ToUtc">The inclusive upper bound of the last-updated timestamp (UTC) to filter by, or <see langword="null"/> to ignore the bound.</param>
/// <param name="IsDeleted">When set, filters to aggregates that are or are not soft-deleted.</param>
/// <param name="IsRestored">When set, filters to aggregates that are or are not restored after deletion.</param>
/// <param name="Page">The one-based page number to return.</param>
/// <param name="PageSize">The maximum number of aggregates to return per page.</param>
/// <param name="Sort">The sort expression, for example <c>"LastUpdatedUtc desc"</c>.</param>
[ZodSchema]
public sealed record AggregateSearchRequest(
	[property: StringLength(256)] string? AggregateType,
	[property: StringLength(256)] string? AggregateId,
	DateTimeOffset? FromUtc,
	DateTimeOffset? ToUtc,
	bool? IsDeleted,
	bool? IsRestored,
	[property: Range(1, int.MaxValue)] int Page = 1,
	[property: Range(1, int.MaxValue)] int PageSize = 50,
	[property: RegularExpression(@"^(?i)[A-Za-z][A-Za-z0-9_.]*\s+(asc|desc)$")] string Sort = "LastUpdatedUtc desc"
);
