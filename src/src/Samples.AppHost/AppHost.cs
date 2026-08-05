var builder = DistributedApplication.CreateBuilder(args);

if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Samples.AppHost v{AssemblyInfo.Version}";

builder.AddAspireResourceKit(onConfigured: kit => SampleAppHostKitOptionsSchema.Validate(kit.Options));

var app = builder.Build();

await app.RunAsync();
