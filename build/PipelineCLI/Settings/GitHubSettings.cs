using ModularPipelines.Attributes;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed record GitHubSettings
{
	public const string SectionName = "GitHub";

	[SecretValue]
	public string? AccessToken { get; init; }

	[SecretValue]
	[ConfigurationKeyName("GITHUB_TOKEN")]
	public string? EnvAccessToken { get; init; }

	public string ProductHeader { get; init; } = "Purview.EventSourcing.Pipeline";

	public string? GetGitHubToken() =>
		!string.IsNullOrWhiteSpace(AccessToken) ? AccessToken
		: !string.IsNullOrWhiteSpace(EnvAccessToken) ? EnvAccessToken
		: null;
}
