var builder = DistributedApplication.CreateBuilder(args);

if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Samples.AppHost v{AssemblyInfo.Version}";

builder.AddAspireResourceKit();

var app = builder.Build();

await app.RunAsync();
