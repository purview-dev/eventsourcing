using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Purview.EventSourcing.Samples;
using Purview.EventSourcing.Samples.Services;
using Purview.EventSourcing.Samples.Web.Services;
using Purview.EventSourcing.SqlServer.Events.Exceptions;

// No authentication in this sample — allow all operations without a principal identifier
EventStoreOperationContext.RequiresValidPrincipalIdentifierDefault = false;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Use Redis when available (e.g. via Aspire AppHost); fall back to in-memory for standalone dev runs
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(Platform.Redis)))
	builder.Services.AddDistributedMemoryCache();
else
	builder.AddRedisDistributedCache(Platform.Redis);

// Register SQL Server event store (event stream + snapshots for querying)
builder.Services.AddSqlServerEventStore(Platform.SqlDatabase);
builder.Services.AddSqlServerSnapshotQueryableEventStore(Platform.SqlDatabase);

builder.Services.AddDomainServices();
builder.Services.AddScoped<IAggregateAuditService, AggregateAuditService>();

// Register product image service — uses Azure Blob Storage when configured, no-op otherwise
var blobConnectionString = builder.Configuration.GetConnectionString(Platform.AzureStorageBlob);
builder.Services.AddSingleton<IProductImageService>(serviceProvider =>
{
	if (string.IsNullOrWhiteSpace(blobConnectionString))
		return new NullProductImageService();

	try
	{
		return new ProductImageService(new BlobServiceClient(blobConnectionString));
	}
	catch (FormatException ex)
	{
		serviceProvider
			.GetRequiredService<ILoggerFactory>()
			.CreateLogger("ProductImageService")
			.LogWarning(ex, "Invalid Azure Blob connection string; product images are disabled.");
		return new NullProductImageService();
	}
});

builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
	app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();
app.MapGroup("/api/audit")
	.MapGet(
		"/aggregates/{aggregateType}/{aggregateId}/events",
		static async Task<IResult> (
			string aggregateType,
			string aggregateId,
			int? fromVersion,
			int? toVersion,
			DateTimeOffset? fromUtc,
			DateTimeOffset? toUtc,
			int? maxRecords,
			string? continuationToken,
			IAggregateAuditService auditService,
			CancellationToken cancellationToken
		) =>
		{
			if (!AggregateAuditService.IsSupportedAggregateType(aggregateType))
				return Results.BadRequest(
					new
					{
						Error = $"Unsupported aggregate type '{aggregateType}'.",
						AggregateAuditService.SupportedAggregateTypes,
					}
				);

			var request = new AggregateEventHistoryRequest
			{
				FromVersion = fromVersion,
				ToVersion = toVersion,
				FromUtc = fromUtc,
				ToUtc = toUtc,
				MaxRecords = maxRecords ?? ContinuationRequest.DefaultMaxRecords,
				ContinuationToken = continuationToken,
			};
			var response = await auditService.GetHistoryAsync(aggregateType, aggregateId, request, cancellationToken);

			return Results.Ok(response);
		}
	);
app.MapDefaultEndpoints().MapGet("/pingz", () => Results.Ok());

// Seed demo data on startup (no-op if data already exists).
await using (var scope = app.Services.CreateAsyncScope())
{
	var seeder = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
	for (var attempt = 0; ; attempt++)
	{
		try
		{
			await seeder.SeedAsync();
			break;
		}
		catch (ConcurrencyException) when (attempt < 2)
		{
			// Another app instance may be seeding the demo store at the same time.
			await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)));
		}
	}
}

await app.RunAsync();
