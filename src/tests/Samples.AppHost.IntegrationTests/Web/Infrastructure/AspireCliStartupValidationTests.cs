using System.Diagnostics;
using System.Text.Json;
using Purview.EventSourcing.Samples;
using Purview.EventSourcing.Samples.Fixtures;

namespace Purview.EventSourcing.Samples.Web.Infrastructure;

[NotInParallel("SamplesAppHost")]
[ClassDataSource<AppHostFixture>(Shared = SharedType.PerTestSession)]
public sealed class AspireCliStartupValidationTests(AppHostFixture fixture)
{
	static readonly string[] AzureVariantResourceNames =
	[
		Platform.AzureSqlWebApp,
		Platform.AzurePostgresWebApp,
		Platform.AzureMongoDbWebApp,
	];

	static readonly string[] ForbiddenStartupLogMarkers =
	[
		"Sample data seeding failed",
		"System.FormatException: No valid combination of account information found.",
		"Unhandled exception",
	];

	static string AppHostProjectPath =>
		Path.GetFullPath(
			Path.Combine(
				AppContext.BaseDirectory,
				"..",
				"..",
				"..",
				"..",
				"..",
				"src",
				"Samples.AppHost",
				"Samples.AppHost.csproj"
			)
		);

	[Test]
	public async Task AzureVariants_StartCleanlyUnderAspireCli_AndServePages(CancellationToken cancellationToken)
	{
		foreach (var resourceName in AzureVariantResourceNames)
		{
			await RunAspireAsync(
				$"wait {resourceName} --apphost \"{AppHostProjectPath}\" --timeout 180 --non-interactive",
				TimeSpan.FromMinutes(4),
				cancellationToken
			);
		}

		var describe = await RunAspireAsync(
			$"describe --apphost \"{AppHostProjectPath}\" --format Json --include-hidden --non-interactive",
			TimeSpan.FromMinutes(2),
			cancellationToken
		);

		foreach (var resourceName in AzureVariantResourceNames)
		{
			var logs = await RunAspireAsync(
				$"logs {resourceName} --apphost \"{AppHostProjectPath}\" --tail 300 --timestamps --non-interactive",
				TimeSpan.FromMinutes(2),
				cancellationToken
			);

			foreach (var marker in ForbiddenStartupLogMarkers)
			{
				if (logs.StandardOutput.Contains(marker, StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException(
						$"Aspire CLI logs for '{resourceName}' contained startup failure marker '{marker}'.{Environment.NewLine}{logs.StandardOutput}"
					);
				}
			}

			var url = GetHttpUrl(describe.StandardOutput, resourceName);
			await Assert.That(url).IsNotNull();

			using var client = fixture.CreateWebClient(resourceName, followRedirects: true);
			var html = await client.GetStringAsync("/", cancellationToken);
			await Assert.That(html).Contains("Customer Experience");
			await Assert.That(html).Contains("Back Office");
		}
	}

	static string? GetHttpUrl(string describeJson, string resourceName)
	{
		using var document = JsonDocument.Parse(describeJson);
		foreach (var resource in document.RootElement.GetProperty("resources").EnumerateArray())
		{
			if (
				!resource.TryGetProperty("displayName", out var displayName)
				|| !string.Equals(displayName.GetString(), resourceName, StringComparison.Ordinal)
			)
			{
				continue;
			}

			if (!resource.TryGetProperty("urls", out var urls))
				return null;

			foreach (var url in urls.EnumerateArray())
			{
				if (
					url.TryGetProperty("url", out var value)
					&& value.GetString() is { } stringValue
					&& stringValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
				)
				{
					return stringValue;
				}
			}

			return null;
		}

		return null;
	}

	static async Task<AspireCliCommandResult> RunAspireAsync(
		string arguments,
		TimeSpan timeout,
		CancellationToken cancellationToken
	)
	{
		using var process = new Process
		{
			StartInfo = new()
			{
				FileName = "aspire",
				Arguments = arguments,
				WorkingDirectory = Path.GetDirectoryName(AppHostProjectPath)!,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		if (!process.Start())
			throw new InvalidOperationException($"Failed to start 'aspire {arguments}'.");

		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeoutCts.CancelAfter(timeout);

		var standardOutputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
		var standardErrorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
		await process.WaitForExitAsync(timeoutCts.Token);

		var result = new AspireCliCommandResult(process.ExitCode, await standardOutputTask, await standardErrorTask);

		if (result.ExitCode != 0)
		{
			throw new InvalidOperationException(
				$"Aspire CLI command failed: aspire {arguments}{Environment.NewLine}STDOUT:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{result.StandardError}"
			);
		}

		return result;
	}

	readonly record struct AspireCliCommandResult(int ExitCode, string StandardOutput, string StandardError);
}
