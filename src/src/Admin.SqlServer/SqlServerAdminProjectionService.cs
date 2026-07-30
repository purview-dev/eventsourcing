using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Admin.Abstractions;
using Purview.EventSourcing.SqlServer.Events;
using Purview.EventSourcing.SqlServer.Events.EntityFramework;

namespace Purview.EventSourcing.Admin.SqlServer;

public sealed class SqlServerAdminProjectionService(
	IOptions<SqlServerEventStoreOptions> options)
	: IAdminProjectionService
{

	public async Task<ProjectionResponse?> ProjectAtVersionAsync(
		string aggregateType,
		string aggregateId,
		long targetVersion,
		CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

		if (targetVersion < 1)
			throw new ArgumentOutOfRangeException(nameof(targetVersion), "Target version must be >= 1");

		var table = SqlServerAdminTableResolver.ResolveTable(options.Value, aggregateType);
		await using var context = CreateContext(options.Value, table);

		var appliedVersions = new List<long>();
		var skippedVersions = new List<long>();
		var projectedState = new Dictionary<string, object>();

		var rows = context.EventStoreEntities.AsNoTracking().Where(x =>
			x.AggregateType == aggregateType &&
			x.AggregateId == aggregateId &&
			x.EntityType == 1 &&
			x.Version <= targetVersion).OrderBy(x => x.Version);

		var rowList = await rows.ToListAsync(cancellationToken);

		foreach (var row in rowList)
		{
			try
			{
				if (row.Payload != null && row.EventType != null)
				{
					using var doc = JsonDocument.Parse(row.Payload);
					var eventPayload = doc.RootElement;
					projectedState[$"event_{row.Version}"] = new { eventType = row.EventType, version = row.Version };
					appliedVersions.Add(row.Version);
				}
				else
				{
					skippedVersions.Add(row.Version);
				}
			}
			catch
			{
				skippedVersions.Add(row.Version);
			}
		}

		if (rowList.Count == 0)
			return null;

		var reason = targetVersion > rowList.Last().Version
			? $"Events projected up to available version {rowList.Last().Version} (target was {targetVersion})"
			: $"Events projected up to version {targetVersion}";

		var finalState = JsonDocument.Parse(JsonSerializer.Serialize(projectedState)).RootElement.Clone();

		return new ProjectionResponse(
			aggregateType,
			aggregateId,
			rowList.Last().Version,
			null,
			finalState,
			new ProjectionProvenance(
				appliedVersions.Count,
				skippedVersions.Count,
				appliedVersions.AsReadOnly(),
				skippedVersions.AsReadOnly(),
				reason));
	}

	public async Task<ProjectionResponse?> ProjectAtTimeAsync(
		string aggregateType,
		string aggregateId,
		DateTimeOffset targetUtc,
		CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

		var table = SqlServerAdminTableResolver.ResolveTable(options.Value, aggregateType);
		await using var context = CreateContext(options.Value, table);

		var appliedVersions = new List<long>();
		var skippedVersions = new List<long>();
		var projectedState = new Dictionary<string, object>();

		var rows = context.EventStoreEntities.AsNoTracking().Where(x =>
			x.AggregateType == aggregateType &&
			x.AggregateId == aggregateId &&
			x.EntityType == 1 &&
			x.Timestamp <= targetUtc).OrderBy(x => x.Version);

		var rowList = await rows.ToListAsync(cancellationToken);

		foreach (var row in rowList)
		{
			try
			{
				if (row.Payload != null && row.EventType != null)
				{
					using var doc = JsonDocument.Parse(row.Payload);
					var eventPayload = doc.RootElement;
					projectedState[$"event_{row.Version}"] = new { eventType = row.EventType, version = row.Version, timestamp = row.Timestamp };
					appliedVersions.Add(row.Version);
				}
				else
				{
					skippedVersions.Add(row.Version);
				}
			}
			catch
			{
				skippedVersions.Add(row.Version);
			}
		}

		if (rowList.Count == 0)
			return null;

		var latestTimestamp = rowList.Last().Timestamp;
		var reason = latestTimestamp > targetUtc
			? $"Events projected up to available timestamp {latestTimestamp:O} (target was {targetUtc:O})"
			: $"Events projected up to timestamp {targetUtc:O}";

		var finalState = JsonDocument.Parse(JsonSerializer.Serialize(projectedState)).RootElement.Clone();

		return new ProjectionResponse(
			aggregateType,
			aggregateId,
			rowList.Last().Version,
			latestTimestamp,
			finalState,
			new ProjectionProvenance(
				appliedVersions.Count,
				skippedVersions.Count,
				appliedVersions.AsReadOnly(),
				skippedVersions.AsReadOnly(),
				reason));
	}

	static EventStoreDbContext CreateContext(SqlServerEventStoreOptions options, SqlServerAdminTableDescriptor table)
	{
		var builder = new DbContextOptionsBuilder<EventStoreDbContext>();
		builder.UseSqlServer(options.ConnectionString);
		return new EventStoreDbContext(builder.Options, table.SchemaName, table.TableName);
	}
}
