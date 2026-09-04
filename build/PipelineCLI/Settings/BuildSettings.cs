using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed class BuildSettings
{
	public const string SectionName = "Build";

	public LogLevel LogLevel { get; init; } = LogLevel.Warning;

	[Required(AllowEmptyStrings = false)]
	public string Solution { get; init; } = "src/EventSourcing.slnx";

	[Required(AllowEmptyStrings = false)]
	public string Configuration { get; init; } = "Release";

	[Required(AllowEmptyStrings = false)]
	public string ArtifactsFolder { get; init; } = "artifacts";

	public bool RunTests { get; init; } = true;

	[Required(AllowEmptyStrings = false)]
	public string TestFilter { get; init; } = "/*/*/*/*/";
}
