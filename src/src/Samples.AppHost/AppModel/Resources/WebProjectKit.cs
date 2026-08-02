using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

static class SampleWebProjectResources
{
	public static IReadOnlyList<SampleWebProjectVariant> AddSampleWebProjects(
		IDistributedApplicationBuilder builder,
		SampleAppHostKit hostKit
	)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(hostKit);

		var options = new SampleWebProjectOptions();
		builder.Configuration.GetSection(SampleWebProjectOptions.SectionName).Bind(options);

		if (options.Variants.Count == 0)
			throw new InvalidOperationException("At least one sample web project variant must be configured.");

		ValidateConfiguration(options);

		var variants = new List<SampleWebProjectVariant>(options.Variants.Count);
		foreach (var configuration in options.Variants)
		{
			variants.Add(new(configuration, AddSampleWebProject(builder, hostKit, configuration, options)));
		}

		AddVariantLinks(variants);
		return variants;
	}

	static void ValidateConfiguration(SampleWebProjectOptions options)
	{
		Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);

		for (var index = 0; index < options.Variants.Count; index++)
		{
			var variant = options.Variants[index];
			try
			{
				Validator.ValidateObject(variant, new ValidationContext(variant), validateAllProperties: true);
			}
			catch (ValidationException ex)
			{
				throw new InvalidOperationException(
					$"Sample web project variant at index {index} is invalid: {ex.ValidationResult?.ErrorMessage ?? ex.Message}",
					ex
				);
			}
		}
	}

	static IResourceBuilder<ProjectResource> AddSampleWebProject(
		IDistributedApplicationBuilder builder,
		SampleAppHostKit hostKit,
		SampleWebProjectConfiguration configuration,
		SampleWebProjectOptions options
	)
	{
		var project = builder.AddProject<Projects.Samples_Web>(configuration.ResourceName).WithExternalHttpEndpoints();

		ConfigureStoreReferences(project, hostKit, configuration);

		if (hostKit.Redis.IsEnabled)
			project.WithReference(hostKit.Redis).WaitFor(hostKit.Redis);

		if (hostKit.AzureStorage.IsEnabled)
			project.WithReference(hostKit.AzureStorage.SnapshotBlob).WaitFor(hostKit.AzureStorage.SnapshotBlob);

		project
			.WithEnvironment($"{SampleStoreOptions.SectionName}__CurrentKey", configuration.Key)
			.WithEnvironment($"{SampleStoreOptions.SectionName}__CurrentDisplayName", configuration.DisplayName)
			.WithEnvironment($"{SampleStoreOptions.SectionName}__CurrentDescription", configuration.Description)
			.WithEnvironment($"{SampleStoreOptions.SectionName}__EventStore", configuration.EventStore.ToString())
			.WithEnvironment($"{SampleStoreOptions.SectionName}__QueryStore", configuration.QueryStore.ToString())
			.WithEnvironment($"{SampleStoreOptions.SectionName}__AdminStore", configuration.AdminStore.ToString())
			.WithEnvironment(
				$"{SampleStoreOptions.SectionName}__AdminApiAvailable",
				configuration.AdminApiAvailable.ToString()
			)
			.WithEnvironment(
				$"{SampleStoreOptions.SectionName}__EventStoreConnectionName",
				configuration.EventStoreConnectionName
			)
			.WithEnvironment(
				$"{SampleStoreOptions.SectionName}__QueryStoreConnectionName",
				configuration.QueryStoreConnectionName
			)
			.WithEnvironment($"{SampleStoreOptions.SectionName}__AdminSitePath", options.AdminSitePath)
			.WithEnvironment($"{SampleStoreOptions.SectionName}__AdminApiPath", options.AdminApiPath)
			.WithEnvironment($"{SampleStoreOptions.SectionName}__DataIsolationWarning", options.DataIsolationWarning);

		if (!string.IsNullOrWhiteSpace(configuration.EventStoreDatabaseName))
		{
			project.WithEnvironment(
				$"{SampleStoreOptions.SectionName}__EventStoreDatabaseName",
				configuration.EventStoreDatabaseName!
			);
		}

		if (!string.IsNullOrWhiteSpace(configuration.QueryStoreDatabaseName))
		{
			project.WithEnvironment(
				$"{SampleStoreOptions.SectionName}__QueryStoreDatabaseName",
				configuration.QueryStoreDatabaseName!
			);
		}

		if (!string.IsNullOrWhiteSpace(configuration.AdminDatabaseName))
		{
			project.WithEnvironment(
				$"{SampleStoreOptions.SectionName}__AdminDatabaseName",
				configuration.AdminDatabaseName!
			);
		}

		return project;
	}

	static void ConfigureStoreReferences(
		IResourceBuilder<ProjectResource> project,
		SampleAppHostKit hostKit,
		SampleWebProjectConfiguration configuration
	)
	{
		var mongoReferenced = false;

		switch (configuration.EventStore)
		{
			case SampleEventStoreKind.SqlServer:
				project
					.WithReference(ResolveSqlServerDatabase(hostKit, configuration.EventStoreConnectionName))
					.WaitFor(ResolveSqlServerDatabase(hostKit, configuration.EventStoreConnectionName));
				break;
			case SampleEventStoreKind.Postgres:
				project
					.WithReference(ResolvePostgresDatabase(hostKit, configuration.EventStoreConnectionName))
					.WaitFor(ResolvePostgresDatabase(hostKit, configuration.EventStoreConnectionName));
				break;
			case SampleEventStoreKind.MongoDb:
				AddMongoReference(project, hostKit, configuration.EventStoreDatabaseName);
				mongoReferenced = true;
				break;
			case SampleEventStoreKind.AzureStorage:
				project.WithReference(hostKit.AzureStorage.TableStorage).WaitFor(hostKit.AzureStorage.TableStorage);
				break;
		}

		switch (configuration.QueryStore)
		{
			case SampleQueryStoreKind.SqlServer:
				project
					.WithReference(ResolveSqlServerDatabase(hostKit, configuration.QueryStoreConnectionName))
					.WaitFor(ResolveSqlServerDatabase(hostKit, configuration.QueryStoreConnectionName));
				break;
			case SampleQueryStoreKind.Postgres:
				project
					.WithReference(ResolvePostgresDatabase(hostKit, configuration.QueryStoreConnectionName))
					.WaitFor(ResolvePostgresDatabase(hostKit, configuration.QueryStoreConnectionName));
				break;
			case SampleQueryStoreKind.MongoDb:
				if (!mongoReferenced)
					project.WithReference(hostKit.MongoDb);

				project.WaitFor(ResolveMongoDatabase(hostKit, configuration.QueryStoreDatabaseName));
				break;
		}
	}

	static void AddMongoReference(
		IResourceBuilder<ProjectResource> project,
		SampleAppHostKit hostKit,
		string? databaseName
	)
	{
		project.WithReference(hostKit.MongoDb);
		project.WaitFor(ResolveMongoDatabase(hostKit, databaseName));
	}

	static IResourceBuilder<SqlServerDatabaseResource> ResolveSqlServerDatabase(
		SampleAppHostKit hostKit,
		string connectionName
	) =>
		connectionName switch
		{
			Platform.SqlDatabase => hostKit.SqlServer.Database,
			Platform.SqlSharedQueryDatabase => hostKit.SqlServer.SharedQueryDatabase,
			_ => throw new InvalidOperationException($"Unsupported SQL Server connection name '{connectionName}'."),
		};

	static IResourceBuilder<PostgresDatabaseResource> ResolvePostgresDatabase(
		SampleAppHostKit hostKit,
		string connectionName
	) =>
		connectionName switch
		{
			Platform.PostgresDatabase => hostKit.Postgres.Database,
			Platform.PostgresSharedQueryDatabase => hostKit.Postgres.SharedQueryDatabase,
			_ => throw new InvalidOperationException($"Unsupported Postgres connection name '{connectionName}'."),
		};

	static IResourceBuilder<MongoDBDatabaseResource> ResolveMongoDatabase(
		SampleAppHostKit hostKit,
		string? databaseName
	) =>
		databaseName switch
		{
			Platform.MongoDatabase => hostKit.MongoDb.Database,
			Platform.MongoSharedQueryDatabase => hostKit.MongoDb.SharedQueryDatabase,
			_ => throw new InvalidOperationException($"Unsupported MongoDB database name '{databaseName}'."),
		};

	public static void AddVariantLinks(IReadOnlyList<SampleWebProjectVariant> variants, string endpointName = "https")
	{
		for (var variantIndex = 0; variantIndex < variants.Count; variantIndex++)
		{
			var variant = variants[variantIndex];
			for (var linkIndex = 0; linkIndex < variants.Count; linkIndex++)
			{
				var link = variants[linkIndex];
				var prefix = $"{SampleStoreOptions.SectionName}__Variants__{linkIndex}";

				variant
					.Resource.WithEnvironment($"{prefix}__Key", link.Configuration.Key)
					.WithEnvironment($"{prefix}__DisplayName", link.Configuration.DisplayName)
					.WithEnvironment($"{prefix}__Description", link.Configuration.Description)
					.WithEnvironment($"{prefix}__Url", link.Resource.GetEndpoint(endpointName));
			}
		}
	}
}

sealed class SampleWebProjectOptions
{
	public const string SectionName = "SampleWebProjects";

	[Required]
	public List<SampleWebProjectConfiguration> Variants { get; set; } = [];

	[Required(AllowEmptyStrings = false)]
	public string AdminSitePath { get; set; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string AdminApiPath { get; set; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string DataIsolationWarning { get; set; } = string.Empty;
}

sealed class SampleWebProjectConfiguration
{
	[Required(AllowEmptyStrings = false)]
	public string Key { get; set; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string ResourceName { get; set; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string DisplayName { get; set; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string Description { get; set; } = string.Empty;

	public SampleEventStoreKind EventStore { get; set; }

	public SampleQueryStoreKind QueryStore { get; set; }

	public SampleAdminStoreKind AdminStore { get; set; }

	public bool AdminApiAvailable { get; set; } = true;

	[Required(AllowEmptyStrings = false)]
	public string EventStoreConnectionName { get; set; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string QueryStoreConnectionName { get; set; } = string.Empty;

	[RegularExpression(@"^[\w\-.]+$")]
	public string? EventStoreDatabaseName { get; set; }

	[RegularExpression(@"^[\w\-.]+$")]
	public string? QueryStoreDatabaseName { get; set; }

	[RegularExpression(@"^[\w\-.]+$")]
	public string? AdminDatabaseName { get; set; }
}

sealed record SampleWebProjectVariant(
	SampleWebProjectConfiguration Configuration,
	IResourceBuilder<ProjectResource> Resource
);
