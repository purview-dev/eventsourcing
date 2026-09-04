using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Internal;
using Purview.EventSourcing.Postgres.Events;
using Purview.EventSourcing.Postgres.Snapshot;
using Purview.EventSourcing.Postgres.Snapshots;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the PostgreSQL event stores with the dependency-injection container.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	static readonly string[] EventsConnectionStringNames =
	[
		"eventstore-events-postgres",
		"EventStore_Events_Postgres",
		"Events_Postgres",
		"eventstore-events-sql",
		"EventStore_Events_Sql",
		"Events_Sql",
	];

	static readonly string[] SnapshotsConnectionStringNames =
	[
		"eventstore-snapshots-postgres",
		"EventStore_Snapshots_Postgres",
		"Snapshots_Postgres",
		"eventstore-snapshots-sql",
		"EventStore_Snapshots_Sql",
		"Snapshots_Sql",
	];

	static readonly string[] DefaultConnectionStringNames =
	[
		"eventstore-postgres",
		"EventStore_Postgres",
		"Postgres",
		"eventstore-sql",
		"EventStore_Sql",
		"Sql",
	];

	extension(IServiceCollection services)
	{
		/// <summary>
		/// Registers the PostgreSQL event store and its dependencies.
		/// </summary>
		/// <param name="connectionStringName">
		/// Optional name of the connection string to use; when null, the first of the well-known
		/// event-store connection string names is used, falling back to the default connection string.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
		public IServiceCollection AddPostgresEventStore(string? connectionStringName = null)
		{
			services.AddEventSourcing();
			services.AddNpgsqlDataSource(connectionStringName ?? EventsConnectionStringNames[0]);
			services.TryAddEnumerable(
				ServiceDescriptor.Singleton<
					IValidateOptions<PostgresEventStoreOptions>,
					PostgresEventStoreOptionsValidator
				>()
			);

			services
				.AddTransient(typeof(IEventStoreCore<>), typeof(PostgresEventStore<>))
				.AddTransient(typeof(INonQueryableEventStore<>), typeof(PostgresEventStore<>))
				.AddTransient(typeof(IPostgresEventStore<>), typeof(PostgresEventStore<>))
				.AddTransient<IEventStore, EventStoreFacade>();
			services.TryAddSingleton<IPostgresEventStoreTransactionFactory, PostgresEventStoreTransactionFactory>();

			services.AddPostgresEventStoreTelemetry();

			services
				.AddOptions<PostgresEventStoreOptions>()
				.Configure<IConfiguration>(
					(options, configuration) =>
					{
						configuration.GetSection(PostgresEventStoreOptions.PostgresEventStore).Bind(options);
						if (string.IsNullOrWhiteSpace(options.ConnectionString))
						{
							options.ConnectionString = configuration.GetRequiredConnectionString([
								connectionStringName,
								.. EventsConnectionStringNames,
								.. DefaultConnectionStringNames,
							]);
						}
					}
				)
				.ValidateOnStart();

			return services;
		}

		/// <summary>
		/// Registers the PostgreSQL queryable snapshot event store and its dependencies.
		/// </summary>
		/// <param name="connectionStringName">
		/// Optional name of the connection string to use; when null, the first of the well-known
		/// snapshot connection string names is used, falling back to the default connection string.
		/// </param>
		/// <param name="registerAsIEventStore">
		/// When <see langword="true"/>, the snapshot store is also registered as an <see cref="IEventStore"/>,
		/// allowing it to be resolved for event-store operations.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
		public IServiceCollection AddPostgresSnapshotQueryableEventStore(
			string? connectionStringName = null,
			bool registerAsIEventStore = false
		)
		{
			services.AddEventSourcing();
			services.AddNpgsqlDataSource(connectionStringName ?? SnapshotsConnectionStringNames[0]);
			services.TryAddEnumerable(
				ServiceDescriptor.Singleton<
					IValidateOptions<PostgresSnapshotEventStoreOptions>,
					PostgresSnapshotEventStoreOptionsValidator
				>()
			);

			services
				.AddTransient(typeof(IQueryableEventStoreCore<>), typeof(PostgresSnapshotEventStore<>))
				.AddTransient(typeof(IPostgresSnapshotEventStore<>), typeof(PostgresSnapshotEventStore<>))
				.AddPostgresSnapshotEventStoreTelemetry();

			services.TryAddTransient<IQueryableEventStore, QueryableEventStoreFacade>();

			if (registerAsIEventStore)
			{
				services.AddTransient(typeof(IEventStoreCore<>), typeof(PostgresSnapshotEventStore<>));
				services.TryAddTransient<IEventStore, EventStoreFacade>();
			}

			services
				.AddOptions<PostgresSnapshotEventStoreOptions>()
				.Configure<IConfiguration>(
					(options, configuration) =>
					{
						configuration.GetSection(PostgresSnapshotEventStoreOptions.PostgresEventStore).Bind(options);
						if (string.IsNullOrWhiteSpace(options.ConnectionString))
						{
							options.ConnectionString = configuration.GetRequiredConnectionString([
								connectionStringName,
								.. SnapshotsConnectionStringNames,
								.. DefaultConnectionStringNames,
							]);
						}
					}
				)
				.ValidateOnStart();

			return services;
		}
	}
}
