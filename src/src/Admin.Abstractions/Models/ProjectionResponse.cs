using System.Text.Json;

namespace Purview.EventSourcing.Admin.Abstractions;

public sealed record ProjectionResponse(
	string AggregateType,
	string AggregateId,
	long ProjectedVersion,
	DateTimeOffset? ProjectedAtUtc,
	JsonElement State,
	ProjectionProvenance Provenance
);
