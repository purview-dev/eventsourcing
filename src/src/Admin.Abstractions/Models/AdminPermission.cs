namespace Purview.EventSourcing.Admin.Abstractions;

public sealed record AdminPermission(
	AdminFeature Feature,
	string? AggregateType,   // null = applies to all aggregate types
	bool Allowed);
