using Purview.EventSourcing.Samples;
using Purview.EventSourcing.Samples.AppHost.AppModel;
using Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

var builder = DistributedApplication.CreateBuilder(args);

if (Environment.UserInteractive)
	Console.Title = $"[{builder.Environment.EnvironmentName}] Samples.AppHost v{AssemblyInfo.Version}";

builder.AddAspireResourceKit();
var hostKit = builder
	.Services.Where(descriptor => descriptor.ServiceType == typeof(SampleAppHostKit))
	.Select(descriptor => descriptor.ImplementationInstance)
	.OfType<SampleAppHostKit>()
	.Single();

SampleWebProjectResources.AddSampleWebProjects(builder, hostKit);

var app = builder.Build();

await app.RunAsync();
