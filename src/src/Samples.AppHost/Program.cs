using Purview.EventSourcing.Samples.AppHost.Services;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddKitAppResourceKit();

var app = builder.Build();

await app.RunAsync();
