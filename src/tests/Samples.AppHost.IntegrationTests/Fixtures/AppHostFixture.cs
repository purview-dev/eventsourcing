using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purview.Aspire.ResourceKit;
using Purview.EventSourcing.Fixtures;
using TUnit.Aspire;

namespace Purview.EventSourcing.Samples.Fixtures;

public sealed class AppHostFixture : AspireFixture<Projects.Samples_AppHost>, IServiceProvider
{
	readonly string _databaseName = $"EventStoreSample_" + $"{Guid.NewGuid():N}"[..8];
	readonly string _snapshotBlobName = $"es-snapshot-" + $"{Guid.NewGuid():N}"[..8];
	static readonly string[] WebResourceNames =
	[
		Platform.WebApp,
		Platform.PostgresWebApp,
		Platform.MongoDbWebApp,
		Platform.AzureSqlWebApp,
		Platform.AzurePostgresWebApp,
		Platform.AzureMongoDbWebApp,
	];

	readonly Lazy<AppServiceHelper> _appService;

	string? _databaseConnectionString;

	public AppHostFixture()
	{
		EventStoreOperationContext.RequiresValidPrincipalIdentifierDefault = false;

		_appService = new(() => new(ConfigureAppServiceHelper));
	}

	protected override string[] Args =>
		[
			.. base.Args,
			.. OptionsHelper
				.ForSet<SampleAppHostKit.SampleAppHostKitOptions>(
					c => c.IsTestRun = true,
					c => c.IsLocal = false,
					c => c.SqlServer.DatabaseName = _databaseName,
					c => c.AzureStorage.BlobName = _snapshotBlobName
				)
				.Build(),
		];

	public override async ValueTask DisposeAsync()
	{
		if (_appService.IsValueCreated)
			await _appService.Value.DisposeAsync();

		await base.DisposeAsync();
	}

	void ConfigureAppServiceHelper(
		IServiceCollection services,
		IConfigurationBuilder configurationBuilder
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(_databaseConnectionString);

		services
			// The event stores...
			.AddSqlServerEventStore(Platform.SqlDatabase)
			.AddSqlServerSnapshotQueryableEventStore(Platform.SqlDatabase)
			// Domain services...
			.AddDomainServices();

		configurationBuilder.AddInMemoryCollection([
			new KeyValuePair<string, string?>(
				$"ConnectionStrings:{Platform.SqlDatabase}",
				_databaseConnectionString
			),
		]);
	}

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();

		_databaseConnectionString = await GetConnectionStringAsync(Platform.SqlDatabase);
		foreach (var resourceName in WebResourceNames)
			await WaitForWebAppAsync(resourceName, CancellationToken.None);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Reliability",
		"CA2000:Dispose objects before losing scope"
	)]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Reliability",
		"CA5399:Do not use HttpClientHandler.AllowAutoRedirect"
	)]
	public HttpClient CreateWebClient(bool followRedirects = false) =>
		CreateWebClient(Platform.WebApp, followRedirects);

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Reliability",
		"CA2000:Dispose objects before losing scope"
	)]
	public HttpClient CreateWebClient(string resourceName, bool followRedirects = false)
	{
		var httpClient = CreateHttpClient(resourceName, "http");
		if (followRedirects)
			return httpClient;

		// We want auto redirect disabled for tests to be able to assert on 302 responses,
		// but HttpClient doesn't allow changing that setting after the client is created,
		// so we create a new client with the same base address and a handler that has auto redirect disabled.
		return new(
			new HttpClientHandler()
			{
				AllowAutoRedirect = false,
				CheckCertificateRevocationList = true,
			}
		)
		{
			BaseAddress = httpClient.BaseAddress,
		};
	}

	public Task<string?> GetResourceConnectionStringAsync(
		string resourceName,
		CancellationToken cancellationToken
	) => GetConnectionStringAsync(resourceName, cancellationToken);

	async Task WaitForWebAppAsync(string resourceName, CancellationToken cancellationToken)
	{
		using var client = CreateWebClient(resourceName, followRedirects: true);
		client.Timeout = TimeSpan.FromSeconds(10);

		var timeoutAt = DateTimeOffset.UtcNow.AddMinutes(3);
		while (DateTimeOffset.UtcNow < timeoutAt)
		{
			try
			{
				using var response = await client.GetAsync("/pingz", cancellationToken);
				if (response.IsSuccessStatusCode)
					return;
			}
			catch (HttpRequestException) when (DateTimeOffset.UtcNow < timeoutAt)
			{
				// Resource may still be starting.
			}
			catch (TaskCanceledException) when (DateTimeOffset.UtcNow < timeoutAt)
			{
				// Resource may still be starting.
			}

			await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
		}

		throw new InvalidOperationException(
			$"The web app resource '{resourceName}' did not become ready in time."
		);
	}

	//string BuildDatabaseConnectionString(string connectionString)
	//{
	//	SqlConnectionStringBuilder builder = new(connectionString) { InitialCatalog = _databaseName };

	//	return builder.ConnectionString;
	//}

	public IQueryableEventStore QueryableEventStore() =>
		_appService.Value.GetRequiredService<IQueryableEventStore>();

	public IEventStore EventStore() => _appService.Value.GetRequiredService<IEventStore>();

	public object? GetService(Type serviceType) => _appService.Value.GetService(serviceType);

	//public object? GetService(Type serviceType) => App.Services.GetService(serviceType);

	//public IQueryableEventStore QueryableEventStore() => App.Services.GetRequiredService<IQueryableEventStore>();

	//public IEventStore EventStore() => App.Services.GetRequiredService<IEventStore>();

	public IServiceProvider CloneServices(Action<IServiceCollection>? configure) =>
		_appService.Value.CloneServices(configure);
}
