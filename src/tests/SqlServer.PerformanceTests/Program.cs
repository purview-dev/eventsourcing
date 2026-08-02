var runBenchmark = Array.Exists(
	args,
	static arg => string.Equals(arg, "--benchmark", StringComparison.OrdinalIgnoreCase)
);

SqlServerStorePerformanceRunner runner = new();
SqlServerStorePerformanceHistoryStore store = new();

using CancellationTokenSource cancellationTokenSource = new();
Console.CancelKeyPress += (s, e) => cancellationTokenSource.Cancel();

var previousRun = store.TryLoadLatest();
var run = await (
	runBenchmark
		? runner.RunBenchmarkAsync(cancellationTokenSource.Token)
		: runner.RunQuickAsync(cancellationTokenSource.Token)
);
var savedPath = await store.SaveAsync(run, cancellationTokenSource.Token);

await Console.Out.WriteLineAsync($"Saved {run.Mode} results to {savedPath}");
await Console.Out.WriteLineAsync();

foreach (var line in run.FormatSummary(previousRun))
	await Console.Out.WriteLineAsync(line);

if (run.Passed)
	return 0;

await Console.Out.WriteLineAsync();
await Console.Error.WriteLineAsync("Performance thresholds were not met.");

return 1;
