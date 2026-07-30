using MongoDB.Driver;
using Purview.EventSourcing.Admin.Abstractions;
using Purview.EventSourcing.MongoDB.Events.Entities;

namespace Purview.EventSourcing.Admin.MongoDB;

public sealed class MongoDbAdminAggregateQueryService(IMongoClient mongoClient, string databaseName)
	: IAdminAggregateQueryService
{
	readonly IMongoClient _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
	readonly string _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));

	public async Task<PagedResult<AggregateSummaryResponse>> SearchAsync(
		AggregateSearchQuery query,
		CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(query);

		var database = _mongoClient.GetDatabase(_databaseName);

		// MongoDB event store uses per-aggregate-type collections with pattern: es-{aggregateType}-events
		// If no aggregate type specified, we can't search effectively across multiple collections
		if (string.IsNullOrWhiteSpace(query.AggregateType))
		{
			// Return empty result when no aggregate type specified
			return new PagedResult<AggregateSummaryResponse>([], query.Page, query.PageSize, 0);
		}

		var collectionName = $"es-{query.AggregateType}-events";
		var collection = database.GetCollection<StreamVersionEntity>(collectionName);

		var filter = Builders<StreamVersionEntity>.Filter.Eq(x => x.EntityType, EntityTypes.StreamVersionType);

		if (!string.IsNullOrWhiteSpace(query.AggregateId))
		{
			filter &= Builders<StreamVersionEntity>.Filter.Eq(x => x.AggregateId, query.AggregateId);
		}

		if (query.FromUtc.HasValue)
		{
			filter &= Builders<StreamVersionEntity>.Filter.Gte(x => x.Timestamp, query.FromUtc.Value);
		}

		if (query.ToUtc.HasValue)
		{
			filter &= Builders<StreamVersionEntity>.Filter.Lte(x => x.Timestamp, query.ToUtc.Value);
		}

		var pageSize = Math.Max(1, Math.Min(query.PageSize, 500));
		var skip = (query.Page - 1) * pageSize;

		var total = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
		var items = await collection
			.Find(filter)
			.Skip(skip)
			.Limit(pageSize)
			.SortByDescending(x => x.Timestamp)
			.ToListAsync(cancellationToken);

		var summaries = items
			.Select(x => new AggregateSummaryResponse(
				query.AggregateType,
				x.AggregateId,
				x.Version,
				x.Timestamp ?? DateTimeOffset.UtcNow,
				x.Timestamp ?? DateTimeOffset.UtcNow,
				x.IsDeleted,
				!x.IsDeleted
			))
			.ToList();

		return new PagedResult<AggregateSummaryResponse>(summaries, query.Page, pageSize, total);
	}

	public async Task<AggregateSummaryResponse?> GetAsync(
		string aggregateType,
		string aggregateId,
		CancellationToken cancellationToken
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

		var database = _mongoClient.GetDatabase(_databaseName);
		var collectionName = $"es-{aggregateType}-events";
		var collection = database.GetCollection<StreamVersionEntity>(collectionName);

		var filter = Builders<StreamVersionEntity>.Filter.And(
			Builders<StreamVersionEntity>.Filter.Eq(x => x.AggregateId, aggregateId),
			Builders<StreamVersionEntity>.Filter.Eq(x => x.EntityType, EntityTypes.StreamVersionType)
		);

		var item = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

		if (item == null)
		{
			return null;
		}

		return new AggregateSummaryResponse(
			aggregateType,
			item.AggregateId,
			item.Version,
			item.Timestamp ?? DateTimeOffset.UtcNow,
			item.Timestamp ?? DateTimeOffset.UtcNow,
			item.IsDeleted,
			!item.IsDeleted
		);
	}
}
