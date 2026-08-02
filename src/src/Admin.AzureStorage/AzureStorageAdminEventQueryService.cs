using System.Text.Json;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Queries;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.AzureStorage.Internal;
using Purview.EventSourcing.AzureStorage;
using Purview.EventSourcing.AzureStorage.Entities;

namespace Purview.EventSourcing.Admin.AzureStorage;

public sealed class AzureStorageAdminEventQueryService(IOptions<AzureStorageEventStoreOptions> options)
	: IAdminEventQueryService
{
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

		var config = options.Value;
		var tableService = AzureStorageAdminTableHelpers.CreateTableServiceClient(config);
		var tableNames = await AzureStorageAdminTableHelpers.ResolveTableNamesAsync(
			tableService,
			config,
			aggregateType,
			cancellationToken
		);

		var rows = new List<(string AggregateType, int Version, EventEntity Event)>();
		foreach (var tableName in tableNames)
		{
			var table = AzureStorageAdminTableHelpers.CreateTableClient(tableService, tableName);
			var filter = AzureStorageAdminTableHelpers.BuildEventRangeFilter(
				aggregateId,
				config.EventPrefix,
				config.EventSuffixLength,
				(int)(query.VersionFrom ?? 1),
				query.VersionTo is null ? null : (int)query.VersionTo.Value
			);

			await foreach (
				var row in table.QueryAsync<EventEntity>(filter, maxPerPage: 100, cancellationToken: cancellationToken)
			)
			{
				if (
					!AzureStorageAdminTableHelpers.TryParseEventVersion(row.RowKey, config.EventPrefix, out var version)
				)
					continue;

				if (query.TimeFromUtc is not null && row.Timestamp < query.TimeFromUtc)
					continue;
				if (query.TimeToUtc is not null && row.Timestamp > query.TimeToUtc)
					continue;

				rows.Add((aggregateType, version, row));
			}
		}

		if (rows.Count == 0)
			return null;

		var directionDesc = query.Sort.Contains("desc", StringComparison.OrdinalIgnoreCase);
		var ordered = directionDesc
			? [.. rows.OrderByDescending(x => x.Version)]
			: rows.OrderBy(x => x.Version).ToList();

		var totalCount = ordered.Count;
		var page = Math.Max(1, query.Page);
		var pageSize = Math.Max(1, query.PageSize);
		var pageRows = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

		var items = pageRows
			.Select(item => new EventEnvelopeResponse(
				item.AggregateType,
				item.Event.PartitionKey,
				new EventMetadataResponse(
					item.Version,
					item.Event.Timestamp ?? DateTimeOffset.MinValue,
					item.Event.EventType ?? string.Empty,
					SchemaVersion: 1,
					CorrelationId: null,
					CausationId: null,
					item.Event.IdempotencyId,
					UserId: null
				),
				ParsePayload(item.Event.Payload)
			))
			.ToList();

		return new PagedResult<EventEnvelopeResponse>(items, page, pageSize, totalCount);
	}

	static JsonElement ParsePayload(string? payload)
	{
		if (string.IsNullOrWhiteSpace(payload))
			return JsonDocument.Parse("null").RootElement.Clone();

		using var document = JsonDocument.Parse(payload);
		return document.RootElement.Clone();
	}
}
