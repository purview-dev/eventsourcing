using System.Text.Json;
using MongoDB.Driver;
using Purview.EventSourcing.Admin.Abstractions;
using Purview.EventSourcing.MongoDB.Events.Entities;

namespace Purview.EventSourcing.Admin.MongoDB;

public sealed class MongoDbAdminEventQueryService(IMongoClient mongoClient, string databaseName)
	: IAdminEventQueryService
{
	readonly IMongoClient _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
	readonly string _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));

	public async Task<PagedResult<EventEnvelopeResponse>?> GetRangeAsync(
		string aggregateType,
		string aggregateId,
		EventRangeQuery query,
		CancellationToken cancellationToken
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
		ArgumentNullException.ThrowIfNull(query);

		var database = _mongoClient.GetDatabase(_databaseName);
		var collectionName = $"es-{aggregateType}-events";
		var collection = database.GetCollection<EventEntity>(collectionName);

		var filter = Builders<EventEntity>.Filter.And(
			Builders<EventEntity>.Filter.Eq(x => x.AggregateId, aggregateId),
			Builders<EventEntity>.Filter.Eq(x => x.EntityType, EntityTypes.EventType)
		);

		if (query.VersionFrom.HasValue && query.VersionFrom > 0)
		{
			filter &= Builders<EventEntity>.Filter.Gte(x => x.Version, query.VersionFrom.Value);
		}

		if (query.VersionTo.HasValue && query.VersionTo > 0)
		{
			filter &= Builders<EventEntity>.Filter.Lte(x => x.Version, query.VersionTo.Value);
		}

		if (query.TimeFromUtc.HasValue)
		{
			filter &= Builders<EventEntity>.Filter.Gte(x => x.Timestamp, query.TimeFromUtc.Value);
		}

		if (query.TimeToUtc.HasValue)
		{
			filter &= Builders<EventEntity>.Filter.Lte(x => x.Timestamp, query.TimeToUtc.Value);
		}

		var pageSize = Math.Max(1, Math.Min(query.PageSize, 500));
		var skip = (query.Page - 1) * pageSize;

		var total = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
		if (total == 0)
		{
			return null;
		}

		var events = await collection
			.Find(filter)
			.Skip(skip)
			.Limit(pageSize)
			.SortBy(x => x.Version)
			.ToListAsync(cancellationToken);

		var envelopes = events
			.Select(e => new EventEnvelopeResponse(
				aggregateType,
				e.AggregateId,
				new EventMetadataResponse(
					e.Version,
					e.Timestamp ?? DateTimeOffset.UtcNow,
					e.EventType ?? "Unknown",
					1,
					null,
					null,
					e.IdempotencyId,
					null
				),
				ParsePayload(e.Payload)
			))
			.ToList();

		return new PagedResult<EventEnvelopeResponse>(envelopes, query.Page, pageSize, total);
	}

	static JsonElement ParsePayload(string? payload)
	{
		if (string.IsNullOrWhiteSpace(payload))
			return JsonDocument.Parse("null").RootElement.Clone();

		using var document = JsonDocument.Parse(payload);
		return document.RootElement.Clone();
	}
}
