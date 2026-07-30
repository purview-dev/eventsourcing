using System.Text.Json;

namespace Purview.EventSourcing.Admin.Abstractions;

public sealed record EventEnvelopeResponse(
	string AggregateType,
	string AggregateId,
	EventMetadataResponse Metadata,
	JsonElement Payload);
