using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Purview.EventSourcing.Admin.Abstractions.Services;

namespace Purview.EventSourcing.Admin.MongoDB;

/// <summary>
/// Registers the MongoDB-backed Admin query and projection services.
/// </summary>
public static class AdminMongoDbServiceCollectionExtensions
{
	/// <summary>
	/// Adds transient <see cref="IAdminAggregateQueryService"/>, <see cref="IAdminEventQueryService"/> and
	/// <see cref="IAdminProjectionService"/> registrations backed by MongoDB.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="databaseName">The name of the database that holds the event store collections. Defaults to <c>EventStore</c>.</param>
	/// <returns>The configured service collection, allowing further chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="databaseName"/> is blank.</exception>
	public static IServiceCollection AddPurviewEventSourcingAdminMongoDB(
		this IServiceCollection services,
		string databaseName = "EventStore"
	)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

		services.TryAddTransient<IAdminAggregateQueryService>(sp =>
		{
			var mongoClient = sp.GetRequiredService<IMongoClient>();
			return new MongoDbAdminAggregateQueryService(mongoClient, databaseName);
		});

		services.TryAddTransient<IAdminEventQueryService>(sp =>
		{
			var mongoClient = sp.GetRequiredService<IMongoClient>();
			return new MongoDbAdminEventQueryService(mongoClient, databaseName);
		});

		services.TryAddTransient<IAdminProjectionService>(sp =>
		{
			var mongoClient = sp.GetRequiredService<IMongoClient>();
			return new MongoDbAdminProjectionService(mongoClient, databaseName);
		});

		return services;
	}
}
