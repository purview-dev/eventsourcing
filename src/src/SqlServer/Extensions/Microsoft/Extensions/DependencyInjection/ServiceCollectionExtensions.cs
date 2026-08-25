using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Internal;
using Purview.EventSourcing.SqlServer.Events;
using Purview.EventSourcing.SqlServer.Snapshot;
using Purview.EventSourcing.SqlServer.Snapshots;

namespace Microsoft.Extensions.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	static readonly string[] EventsConnectionStringNames =
	[
		"eventstore-events-sqlserver",
		"EventStore_Events_SqlServer",
		"Events_SqlServer",
		"eventstore-events-sql",
		"EventStore_Events_Sql",
		"Events_Sql",
	];

	static readonly string[] SnapshotsConnectionStringNames =
	[
		"eventstore-snapshots-sqlserver",
		"EventStore_Snapshots_SqlServer",
		"Snapshots_SqlServer",
		"eventstore-snapshots-sql",
		"EventStore_Snapshots_Sql",
		"Snapshots_Sql",
	];

	static readonly string[] DefaultConnectionStringNames =
	[
		"eventstore-sqlserver",
		"EventStore_SqlServer",
		"SqlServer",
		"eventstore-sql",
		"EventStore_Sql",
		"Sql",
	];

	extension(IServiceCollection services)
	{
		public IServiceCollection AddSqlServerEventStore(string? connectionStringName = null)
		{
			services.AddEventSourcing();
			services.TryAddEnumerable(
				ServiceDescriptor.Singleton<
					IValidateOptions<SqlServerEventStoreOptions>,
					SqlServerEventStoreOptionsValidator
				>()
			);

			services
				.AddTransient(typeof(IEventStoreCore<>), typeof(SqlServerEventStore<>))
				.AddTransient(typeof(INonQueryableEventStore<>), typeof(SqlServerEventStore<>))
				.AddTransient(typeof(ISqlServerEventStore<>), typeof(SqlServerEventStore<>))
				.AddTransient<IEventStore, EventStoreFacade>();
			services.TryAddSingleton<ISqlServerEventStoreTransactionFactory, SqlServerEventStoreTransactionFactory>();

			services.AddSqlServerEventStoreTelemetry();

			services
				.AddOptions<SqlServerEventStoreOptions>()
				.Configure<IConfiguration>(
					(options, configuration) =>
					{
						configuration.GetSection(SqlServerEventStoreOptions.SqlServerEventStore).Bind(options);
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

		public IServiceCollection AddSqlServerSnapshotQueryableEventStore(
			string? connectionStringName = null,
			bool registerAsIEventStore = false
		)
		{
			services.AddEventSourcing();
			services.TryAddEnumerable(
				ServiceDescriptor.Singleton<
					IValidateOptions<SqlServerSnapshotEventStoreOptions>,
					SqlServerSnapshotEventStoreOptionsValidator
				>()
			);

			services
				.AddTransient(typeof(IQueryableEventStoreCore<>), typeof(SqlServerSnapshotEventStore<>))
				.AddTransient(typeof(ISqlServerSnapshotEventStore<>), typeof(SqlServerSnapshotEventStore<>))
				.AddSqlServerSnapshotEventStoreTelemetry();

			services.TryAddTransient<IQueryableEventStore, QueryableEventStoreFacade>();

			if (registerAsIEventStore)
			{
				services.AddTransient(typeof(IEventStoreCore<>), typeof(SqlServerSnapshotEventStore<>));
				services.TryAddTransient<IEventStore, EventStoreFacade>();
			}

			services
				.AddOptions<SqlServerSnapshotEventStoreOptions>()
				.Configure<IConfiguration>(
					(options, configuration) =>
					{
						configuration.GetSection(SqlServerSnapshotEventStoreOptions.SqlServerEventStore).Bind(options);
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
