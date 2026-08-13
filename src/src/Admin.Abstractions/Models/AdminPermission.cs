namespace Purview.EventSourcing.Admin.Abstractions.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Naming",
	"CA1711:Identifiers should not have incorrect suffix"
)]
public sealed record AdminPermission(
	AdminFeature Feature,
	string? AggregateType, // null = applies to all aggregate types
	bool Allowed
);
