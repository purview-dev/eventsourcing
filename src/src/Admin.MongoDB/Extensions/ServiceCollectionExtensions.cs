using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Purview.EventSourcing.Admin.Abstractions;

namespace Purview.EventSourcing.Admin.MongoDB;

public static class AdminMongoDbServiceCollectionExtensions
{
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
