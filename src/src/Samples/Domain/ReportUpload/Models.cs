namespace Purview.EventSourcing.Samples.Domain.ReportUpload;

public sealed class ParserReportSummary
{
	public required ParserDetails ParserDetails { get; init; }

	public required VulnerabilityDetails VulnerabilityDetails { get; init; }

	public required AssetDetails AssetDetails { get; init; }

	public required IEnumerable<Project> Projects { get; init; } = [];
}

public sealed record ParserDetails(int TotalLines, int SuccessfulLines, int FailedLines, TimeSpan ProcessingTime)
{
	public bool HasFailures => FailedLines > 0;

	public bool TotalSuccess => SuccessfulLines == TotalLines;

	public bool TotalFailure => FailedLines == TotalLines;

	public bool PartialSuccess => SuccessfulLines > 0 && FailedLines > 0;

	public bool Successful => SuccessfulLines > 0 && FailedLines == 0;
}

public sealed record VulnerabilityDetails(
	int TotalVulnerabilities,
	int UniqueVulnerabilities,
	int CriticalVulnerabilities,
	int HighVulnerabilities,
	int MediumVulnerabilities,
	int LowVulnerabilities
);

public sealed record AssetDetails(Dictionary<PlatformID, int> OperatingSystemDistribution)
{
	public int TotalAssets => OperatingSystemDistribution.Values.Sum();
}

public sealed record Project(string Name, string Version, string Team);
