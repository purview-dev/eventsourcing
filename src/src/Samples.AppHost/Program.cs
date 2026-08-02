using Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

var builder = DistributedApplication.CreateBuilder(args);

if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Samples.AppHost v{AssemblyInfo.Version}";

builder.AddAspireResourceKit(onConfigured: kit => SampleWebProjectResources.AddSampleWebProjects(builder, kit));

var app = builder.Build();

await app.RunAsync();
