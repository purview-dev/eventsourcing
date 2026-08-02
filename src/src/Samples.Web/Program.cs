using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication;
using MongoDB.Driver;
using Npgsql;
using Purview.EventSourcing.Admin.Api;
using Purview.EventSourcing.Admin.AzureStorage;
using Purview.EventSourcing.Admin.MongoDB;
using Purview.EventSourcing.Admin.Postgres;
using Purview.EventSourcing.Admin.Security;
using Purview.EventSourcing.Admin.Site;
using Purview.EventSourcing.Admin.SqlServer;
using Purview.EventSourcing.AzureStorage;
using Purview.EventSourcing.MongoDB.Events;
using Purview.EventSourcing.MongoDB.Snapshots;
using Purview.EventSourcing.Samples;
using Purview.EventSourcing.Samples.Services;
using Purview.EventSourcing.Samples.Web.Services;
using AzureCommitException = Purview.EventSourcing.AzureStorage.Exceptions.CommitException;
using AzureConcurrencyException = Purview.EventSourcing.AzureStorage.Exceptions.ConcurrencyException;
using SqlServerCommitException = Purview.EventSourcing.SqlServer.Events.Exceptions.CommitException;
using SqlServerConcurrencyException = Purview.EventSourcing.SqlServer.Events.Exceptions.ConcurrencyException;

// No authentication in this sample — allow all operations without a principal identifier
EventStoreOperationContext.RequiresValidPrincipalIdentifierDefault = false;

var builder = WebApplication.CreateBuilder(args);
var sampleStoreOptions =
	builder.Configuration.GetSection(SampleStoreOptions.SectionName).Get<SampleStoreOptions>() ?? new();

builder.AddServiceDefaults();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddSingleton(sampleStoreOptions);

// Use Redis when available (e.g. via Aspire AppHost); fall back to in-memory for standalone dev runs
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(Platform.Redis)))
	builder.Services.AddDistributedMemoryCache();
else
	builder.AddRedisDistributedCache(Platform.Redis);

ConfigureStoreOptions(builder.Services, builder.Configuration, sampleStoreOptions);
RegisterEventStore(builder.Services, sampleStoreOptions);
RegisterQueryStore(builder.Services, sampleStoreOptions);
RegisterAdmin(builder.Services, sampleStoreOptions);

builder.Services.AddDomainServices();
builder.Services.AddScoped<IAggregateAuditService, AggregateAuditService>();

// Register product image service — uses Azure Blob Storage when configured, no-op otherwise
var blobConnectionString = NormalizeAzureStorageConnectionString(
	builder.Configuration.GetConnectionString(Platform.AzureStorageBlob)
);
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
	catch (ArgumentException ex)
	{
		serviceProvider
			.GetRequiredService<ILoggerFactory>()
			.CreateLogger("ProductImageService")
			.LogWarning(ex, "Invalid Azure Blob connection string; product images are disabled.");
		return new NullProductImageService();
	}
});

if (sampleStoreOptions.AdminApiAvailable)
	builder.Services.AddPurviewEventSourcingAdminSite(
		enableRazorRuntimeCompilation: builder.Environment.IsDevelopment()
	);

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
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

if (sampleStoreOptions.AdminApiAvailable)
{
	app.MapPurviewEventSourcingAdminApi();
	app.MapPurviewEventSourcingAdminSite();
}

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
	var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");
	for (var attempt = 0; ; attempt++)
	{
		try
		{
			await seeder.SeedAsync();
			break;
		}
		catch (SqlServerConcurrencyException) when (attempt < 2)
		{
			// Another app instance may be seeding the demo store at the same time.
			await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)));
		}
		catch (AzureConcurrencyException) when (attempt < 2)
		{
			// Another app instance may be seeding the demo store at the same time.
			await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)));
		}
		catch (RequestFailedException ex)
		{
			logger.LogError(
				ex,
				"Sample data seeding failed for store variant '{StoreVariant}'.",
				sampleStoreOptions.CurrentKey
			);
			break;
		}
		catch (MongoException ex)
		{
			logger.LogError(
				ex,
				"Sample data seeding failed for store variant '{StoreVariant}'.",
				sampleStoreOptions.CurrentKey
			);
			break;
		}
		catch (NpgsqlException ex)
		{
			logger.LogError(
				ex,
				"Sample data seeding failed for store variant '{StoreVariant}'.",
				sampleStoreOptions.CurrentKey
			);
			break;
		}
		catch (SqlServerCommitException ex)
		{
			logger.LogError(
				ex,
				"Sample data seeding failed for store variant '{StoreVariant}'.",
				sampleStoreOptions.CurrentKey
			);
			break;
		}
		catch (AzureCommitException ex)
		{
			logger.LogError(
				ex,
				"Sample data seeding failed for store variant '{StoreVariant}'.",
				sampleStoreOptions.CurrentKey
			);
			break;
		}
		catch (FormatException ex)
		{
			logger.LogError(
				ex,
				"Sample data seeding failed for store variant '{StoreVariant}'.",
				sampleStoreOptions.CurrentKey
			);
			break;
		}
		catch (ArgumentException ex)
		{
			logger.LogError(
				ex,
				"Sample data seeding failed for store variant '{StoreVariant}'.",
				sampleStoreOptions.CurrentKey
			);
			break;
		}
		catch (InvalidOperationException ex)
		{
			logger.LogError(
				ex,
				"Sample data seeding failed for store variant '{StoreVariant}'.",
				sampleStoreOptions.CurrentKey
			);
			break;
		}
	}
}

await app.RunAsync();

static void ConfigureStoreOptions(
	IServiceCollection services,
	IConfiguration configuration,
	SampleStoreOptions sampleStoreOptions
)
{
	switch (sampleStoreOptions.EventStore)
	{
		case SampleEventStoreKind.MongoDb:
			services
				.AddOptions<MongoDBEventStoreOptions>()
				.Configure(options =>
					options.Database = sampleStoreOptions.EventStoreDatabaseName ?? Platform.MongoDatabase
				);
			break;
		case SampleEventStoreKind.AzureStorage:
			services
				.AddOptions<AzureStorageEventStoreOptions>()
				.Configure(options =>
				{
					options.Table = $"EventStore{NormalizeAlphaNumeric(sampleStoreOptions.CurrentKey)}";
					options.Container = $"eventstore-{NormalizeKebab(sampleStoreOptions.CurrentKey)}";
				});
			services.PostConfigure<AzureStorageEventStoreOptions>(options =>
				options.ConnectionString = BuildAzureEventStoreConnectionString(
					configuration.GetConnectionString(sampleStoreOptions.EventStoreConnectionName),
					configuration.GetConnectionString(Platform.AzureStorageBlob),
					options.ConnectionString
				)
			);
			break;
	}

	if (sampleStoreOptions.QueryStore == SampleQueryStoreKind.MongoDb)
	{
		services
			.AddOptions<MongoDBSnapshotEventStoreOptions>()
			.Configure(options =>
				options.Database = sampleStoreOptions.QueryStoreDatabaseName ?? Platform.MongoDatabase
			);
	}
}

static void RegisterEventStore(IServiceCollection services, SampleStoreOptions sampleStoreOptions)
{
	switch (sampleStoreOptions.EventStore)
	{
		case SampleEventStoreKind.SqlServer:
			services.AddSqlServerEventStore(sampleStoreOptions.EventStoreConnectionName);
			break;
		case SampleEventStoreKind.Postgres:
			services.AddPostgresEventStore(sampleStoreOptions.EventStoreConnectionName);
			break;
		case SampleEventStoreKind.MongoDb:
			services.AddMongoDBEventStore(sampleStoreOptions.EventStoreConnectionName);
			break;
		case SampleEventStoreKind.AzureStorage:
			services.AddAzureStorageEventStore(sampleStoreOptions.EventStoreConnectionName);
			break;
		default:
			throw new InvalidOperationException($"Unsupported event store '{sampleStoreOptions.EventStore}'.");
	}
}

static void RegisterQueryStore(IServiceCollection services, SampleStoreOptions sampleStoreOptions)
{
	switch (sampleStoreOptions.QueryStore)
	{
		case SampleQueryStoreKind.SqlServer:
			services.AddSqlServerSnapshotQueryableEventStore(sampleStoreOptions.QueryStoreConnectionName);
			break;
		case SampleQueryStoreKind.Postgres:
			services.AddPostgresSnapshotQueryableEventStore(sampleStoreOptions.QueryStoreConnectionName);
			break;
		case SampleQueryStoreKind.MongoDb:
			services.AddMongoDBSnapshotQueryableEventStore(sampleStoreOptions.QueryStoreConnectionName);
			break;
		default:
			throw new InvalidOperationException($"Unsupported query store '{sampleStoreOptions.QueryStore}'.");
	}
}

static void RegisterAdmin(IServiceCollection services, SampleStoreOptions sampleStoreOptions)
{
	if (!sampleStoreOptions.AdminApiAvailable)
		return;

	services
		.AddAuthentication(SampleAdminAuthenticationHandler.SchemeName)
		.AddScheme<AuthenticationSchemeOptions, SampleAdminAuthenticationHandler>(
			SampleAdminAuthenticationHandler.SchemeName,
			configureOptions: null
		);
	services.AddAuthorizationBuilder().AddPurviewEventSourcingAdminPolicies();
	services.AddPurviewEventSourcingAdminSecurity(new SampleAdminPermissionProvider());
	services.AddPurviewEventSourcingAdminApi(options => options.RoutePrefix = sampleStoreOptions.AdminApiPath);

	switch (sampleStoreOptions.AdminStore)
	{
		case SampleAdminStoreKind.SqlServer:
			services.AddPurviewEventSourcingAdminSqlServer();
			break;
		case SampleAdminStoreKind.Postgres:
			services.AddPurviewEventSourcingAdminPostgres();
			break;
		case SampleAdminStoreKind.MongoDb:
			services.AddPurviewEventSourcingAdminMongoDB(
				sampleStoreOptions.AdminDatabaseName
					?? sampleStoreOptions.EventStoreDatabaseName
					?? sampleStoreOptions.QueryStoreDatabaseName
					?? Platform.MongoDatabase
			);
			break;
		case SampleAdminStoreKind.AzureStorage:
			services.AddPurviewEventSourcingAdminAzureStorage();
			break;
		default:
			throw new InvalidOperationException($"Unsupported admin store '{sampleStoreOptions.AdminStore}'.");
	}
}

static string NormalizeAlphaNumeric(string value) => new([.. value.Where(char.IsLetterOrDigit)]);

[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
static string NormalizeKebab(string value)
{
	var normalized = new string([
		.. value.ToLowerInvariant().Where(character => char.IsLetterOrDigit(character) || character == '-'),
	]);

	return string.IsNullOrWhiteSpace(normalized) ? "sample" : normalized;
}

static string NormalizeConnectionString(string? value)
{
	if (string.IsNullOrWhiteSpace(value))
		return string.Empty;

	var parts = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();

	return parts.Length == 0 ? string.Empty : string.Join(';', parts);
}

static string NormalizeAzureStorageConnectionString(string? value)
{
	var normalized = NormalizeConnectionString(value);
	if (string.IsNullOrWhiteSpace(normalized))
		return normalized;

	if (normalized.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
		return "UseDevelopmentStorage=true";

	var parts = normalized
		.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
		.Select(part => part.Split('=', 2))
		.Where(part => part.Length == 2)
		.Select(part => new KeyValuePair<string, string>(part[0], part[1]))
		.ToList();

	var map = parts.ToDictionary(part => part.Key, part => part.Value, StringComparer.OrdinalIgnoreCase);

	if (
		!map.TryGetValue("BlobEndpoint", out var blobEndpointRaw)
		|| !map.TryGetValue("AccountName", out var accountName)
		|| !Uri.TryCreate(blobEndpointRaw, UriKind.Absolute, out var blobEndpoint)
	)
	{
		return normalized;
	}

	var basePath = $"/{accountName}";
	var trimmedPath = blobEndpoint.AbsolutePath.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase)
		? basePath
		: blobEndpoint.AbsolutePath;
	var blobBuilder = new UriBuilder(blobEndpoint) { Path = trimmedPath };
	map["BlobEndpoint"] = blobBuilder.Uri.ToString().TrimEnd('/');

	foreach (var key in map.Keys.ToArray())
	{
		var index = parts.FindIndex(part => string.Equals(part.Key, key, StringComparison.OrdinalIgnoreCase));
		if (index >= 0)
			parts[index] = new KeyValuePair<string, string>(key, map[key]);
		else
			parts.Add(new KeyValuePair<string, string>(key, map[key]));
	}

	return string.Join(';', parts.Select(part => $"{part.Key}={part.Value}"));
}

static string BuildAzureEventStoreConnectionString(
	string? eventStoreConnectionString,
	string? blobConnectionString,
	string? fallbackConnectionString
)
{
	var eventStore = ParseConnectionStringParts(NormalizeAzureStorageConnectionString(eventStoreConnectionString));
	var blob = ParseConnectionStringParts(NormalizeAzureStorageConnectionString(blobConnectionString));
	var fallback = ParseConnectionStringParts(NormalizeAzureStorageConnectionString(fallbackConnectionString));

	if (
		ContainsUseDevelopmentStorage(eventStore)
		|| ContainsUseDevelopmentStorage(blob)
		|| ContainsUseDevelopmentStorage(fallback)
	)
	{
		return "UseDevelopmentStorage=true";
	}

	var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	void Merge(IDictionary<string, string> source)
	{
		foreach (var (key, value) in source)
		{
			if (!string.IsNullOrWhiteSpace(value))
				merged[key] = value;
		}
	}

	Merge(fallback);
	Merge(blob);
	Merge(eventStore);

	if (
		!merged.ContainsKey("BlobEndpoint")
		&& merged.TryGetValue("TableEndpoint", out var tableEndpointRaw)
		&& Uri.TryCreate(tableEndpointRaw, UriKind.Absolute, out var tableEndpoint)
	)
	{
		var blobBuilder = new UriBuilder(tableEndpoint);
		if (blobBuilder.Port == 10002)
			blobBuilder.Port = 10000;
		merged["BlobEndpoint"] = blobBuilder.Uri.ToString().TrimEnd('/');
	}

	if (
		!merged.ContainsKey("TableEndpoint")
		&& merged.TryGetValue("BlobEndpoint", out var blobEndpointRaw)
		&& Uri.TryCreate(blobEndpointRaw, UriKind.Absolute, out var blobEndpoint)
	)
	{
		var tableBuilder = new UriBuilder(blobEndpoint);
		if (tableBuilder.Port == 10000)
			tableBuilder.Port = 10002;
		merged["TableEndpoint"] = tableBuilder.Uri.ToString().TrimEnd('/');
	}

	return string.Join(';', merged.Select(part => $"{part.Key}={part.Value}"));
}

static Dictionary<string, string> ParseConnectionStringParts(string? connectionString)
{
	if (string.IsNullOrWhiteSpace(connectionString))
		return [];

	var pairs = connectionString
		.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
		.Select(part => part.Split('=', 2))
		.Where(part => part.Length == 2);

	var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	foreach (var pair in pairs)
		values[pair[0]] = pair[1];

	return values;
}

static bool ContainsUseDevelopmentStorage(IReadOnlyDictionary<string, string> values) =>
	values.TryGetValue("UseDevelopmentStorage", out var value)
	&& string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
