namespace Purview.EventSourcing.Admin.Abstractions.Models;

public sealed record ProjectionProvenance(
	int AppliedCount,
	int SkippedCount,
	IReadOnlyList<long> AppliedVersions,
	IReadOnlyList<long> SkippedVersions,
	string Reason
);
