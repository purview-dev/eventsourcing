using Aspire.Hosting.ApplicationModel;
using Purview.EventSourcing.Samples;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

static class SampleWebProjectResources
{
	public static IResourceBuilder<ProjectResource> AddSampleWebProject(
		IDistributedApplicationBuilder builder,
		SampleAppHostKit hostKit,
		string resourceName,
		SampleWebProjectConfiguration configuration,
		Action<IResourceBuilder<ProjectResource>> configureReferences
	)
	{
		var project = builder.AddProject<Projects.Samples_Web>(resourceName).WithExternalHttpEndpoints();

		configureReferences(project);

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
			.WithEnvironment($"{SampleStoreOptions.SectionName}__AdminSitePath", "/admin")
			.WithEnvironment($"{SampleStoreOptions.SectionName}__AdminApiPath", "/admin/api")
			.WithEnvironment(
				$"{SampleStoreOptions.SectionName}__DataIsolationWarning",
				"Each sample option uses isolated backing data. Switching store types changes the seeded dataset that you see."
			);

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

sealed record SampleWebProjectConfiguration(
	string Key,
	string DisplayName,
	string Description,
	SampleEventStoreKind EventStore,
	SampleQueryStoreKind QueryStore,
	SampleAdminStoreKind AdminStore,
	bool AdminApiAvailable,
	string EventStoreConnectionName,
	string QueryStoreConnectionName,
	string? EventStoreDatabaseName = null,
	string? QueryStoreDatabaseName = null,
	string? AdminDatabaseName = null
);

sealed record SampleWebProjectVariant(
	SampleWebProjectConfiguration Configuration,
	IResourceBuilder<ProjectResource> Resource
);
