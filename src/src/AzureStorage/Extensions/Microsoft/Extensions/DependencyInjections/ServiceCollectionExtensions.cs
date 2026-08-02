using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Purview.EventSourcing.Internal;

namespace Microsoft.Extensions.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		public IServiceCollection AddAzureStorageEventStore() =>
			services.AddAzureStorageEventStore(connectionStringName: null);

		public IServiceCollection AddAzureStorageEventStore(string? connectionStringName)
		{
			services.AddEventSourcing();

			services
				.AddTransient(typeof(IEventStoreCore<>), typeof(TableEventStore<>))
				.AddTransient(typeof(INonQueryableEventStore<>), typeof(TableEventStore<>))
				.AddTransient(typeof(ITableEventStore<>), typeof(TableEventStore<>))
				.AddTransient<IEventStore, EventStoreFacade>()
				.AddTableEventStoreTelemetry();

			services
				.AddOptions<AzureStorageEventStoreOptions>()
				.Configure<IConfiguration>(
					(options, configuration) =>
					{
						configuration.GetSection(AzureStorageEventStoreOptions.AzureStorageEventStore).Bind(options);

						if (string.IsNullOrWhiteSpace(options.ConnectionString))
						{
							options.ConnectionString = configuration.GetRequiredConnectionString([
								connectionStringName,
								"EventStore_AzureStorage",
								"AzureStorage",
							]);
						}
					}
				)
				.Validate(
					static options =>
						Validator.TryValidateObject(
							options,
							new ValidationContext(options),
							validationResults: null,
							validateAllProperties: true
						),
					$"{nameof(AzureStorageEventStoreOptions)} is invalid."
				)
				.ValidateOnStart();

			return services;
		}
	}
}
