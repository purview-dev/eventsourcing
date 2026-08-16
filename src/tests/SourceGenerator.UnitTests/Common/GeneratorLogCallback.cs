using Purview.SourceGeneratorFramework.Logging;

namespace Purview.EventSourcing.SourceGenerator.Common;

public static class GeneratorLogCallback
{
	public static Action<string, OutputType> Create(bool throwOnLogError)
	{
		return (message, outputType) =>
		{
			var prefix = outputType switch
			{
				OutputType.Diagnostic => "DIA",
				OutputType.Debug => "DBG",
				OutputType.Info => "INF",
				OutputType.Warning => "WRN",
				OutputType.Error => "ERR",
				_ => "???",
			};

			TestContext.Current.OutputWriter.WriteLine($"{prefix}: {message}");

			if (throwOnLogError && outputType == OutputType.Error)
				throw new InvalidOperationException($"Generator logged error: {message}");
		};
	}
}
