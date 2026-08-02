namespace Purview.EventSourcing.Admin.Abstractions.Models;

public sealed record EventMetadataResponse(
	long Version,
	DateTimeOffset TimestampUtc,
	string EventType,
	int SchemaVersion,
	string? CorrelationId,
	string? CausationId,
	string? IdempotencyId,
	string? UserId
);
