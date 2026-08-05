using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Purview.EventSourcing.Internal;
using Purview.EventSourcing.MongoDB.Events;
using Purview.EventSourcing.MongoDB.Snapshots;

namespace Microsoft.Extensions.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	extension([NotNull] IServiceCollection services)
	{
		public IServiceCollection AddMongoDBEventStore() => services.AddMongoDBEventStore(connectionStringName: null);

		public IServiceCollection AddMongoDBEventStore(string? connectionStringName)
		{
			services.AddEventSourcing();

			services
				.AddTransient(typeof(IEventStoreCore<>), typeof(MongoDBEventStore<>))
				.AddTransient(typeof(INonQueryableEventStore<>), typeof(MongoDBEventStore<>))
				.AddTransient(typeof(IMongoDBEventStore<>), typeof(MongoDBEventStore<>))
				.AddTransient<IEventStore, EventStoreFacade>()
				.AddMongoDBEventStoreTelemetry();

			services.AddMongoDBClientTelemetry();

			services
				.AddOptions<MongoDBEventStoreOptions>()
				.Configure<IConfiguration>(
					(options, configuration) =>
					{
						configuration.GetSection(MongoDBEventStoreOptions.MongoDBEventStore).Bind(options);

						if (string.IsNullOrWhiteSpace(options.ConnectionString))
						{
							options.ConnectionString = configuration.GetRequiredConnectionString([
								connectionStringName,
								"EventStore_MongoDB",
								"MongoDB",
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
					$"{nameof(MongoDBEventStoreOptions)} is invalid."
				)
				.ValidateOnStart();

			services.TryAddSingleton<IMongoClient>(sp =>
			{
				var options = sp.GetRequiredService<IOptions<MongoDBEventStoreOptions>>().Value;
				var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
				if (!string.IsNullOrWhiteSpace(options.ApplicationName))
					settings.ApplicationName = options.ApplicationName;

				return new MongoClient(settings);
			});

			return services;
		}

		public IServiceCollection AddMongoDBSnapshotQueryableEventStore(bool registerAsIEventStore = false) =>
			services.AddMongoDBSnapshotQueryableEventStore(connectionStringName: null, registerAsIEventStore);

		public IServiceCollection AddMongoDBSnapshotQueryableEventStore(
			string? connectionStringName,
			bool registerAsIEventStore = false
		)
		{
			services.AddEventSourcing();

			services
				.AddTransient(typeof(IQueryableEventStoreCore<>), typeof(MongoDBSnapshotEventStore<>))
				.AddTransient(typeof(IMongoDBSnapshotEventStore<>), typeof(MongoDBSnapshotEventStore<>))
				.AddMongoDBSnapshotEventStoreTelemetry();
			services.AddMongoDBClientTelemetry();

			services.TryAddTransient<IQueryableEventStore, QueryableEventStoreFacade>();

			if (registerAsIEventStore)
			{
				services.AddTransient(typeof(IEventStoreCore<>), typeof(MongoDBSnapshotEventStore<>));
				services.TryAddTransient<IEventStore, EventStoreFacade>();
			}

			services
				.AddOptions<MongoDBSnapshotEventStoreOptions>()
				.Configure<IConfiguration>(
					(options, configuration) =>
					{
						configuration.GetSection(MongoDBSnapshotEventStoreOptions.MongoDBEventStore).Bind(options);

						options.ConnectionString ??= configuration.GetRequiredConnectionString([
							connectionStringName,
							"EventStore_MongoDBSnapshot",
							"MongoDBSnapshot",
							"EventStore_MongoDB",
							"MongoDB",
						]);
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
					$"{nameof(MongoDBSnapshotEventStoreOptions)} is invalid."
				)
				.ValidateOnStart();

			services.TryAddSingleton<IMongoClient>(sp =>
			{
				var options = sp.GetRequiredService<IOptions<MongoDBSnapshotEventStoreOptions>>().Value;
				var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
				if (!string.IsNullOrWhiteSpace(options.ApplicationName))
					settings.ApplicationName = options.ApplicationName;

				return new MongoClient(settings);
			});

			return services;
		}
	}
}
