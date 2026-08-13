using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Queries;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.SqlServer.Internal;
using Purview.EventSourcing.SqlServer.Events;
using Purview.EventSourcing.SqlServer.Events.EntityFramework;

namespace Purview.EventSourcing.Admin.SqlServer;

public sealed class SqlServerAdminAggregateQueryService(
	IOptions<SqlServerEventStoreOptions> options
) : IAdminAggregateQueryService
{
	public async Task<PagedResult<AggregateSummaryResponse>> SearchAsync(
		AggregateSearchQuery query,
		CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(query);

		var page = Math.Max(1, query.Page);
		var pageSize = Math.Max(1, query.PageSize);

		var candidates = new List<AggregateSummaryResponse>();
		foreach (
			var table in SqlServerAdminTableResolver.ResolveTables(
				options.Value,
				query.AggregateType
			)
		)
		{
			await using var context = CreateContext(options.Value, table);
			var rows = await BuildAggregateRowsAsync(context, table, query, cancellationToken);
			candidates.AddRange(rows);
		}

		var ordered = ApplySort(candidates, query.Sort).ToList();
		var totalCount = ordered.Count;
		var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

		return new PagedResult<AggregateSummaryResponse>(pageItems, page, pageSize, totalCount);
	}

	public async Task<AggregateSummaryResponse?> GetAsync(
		string aggregateType,
		string aggregateId,
		CancellationToken cancellationToken
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

		var query = new AggregateSearchQuery(
			aggregateType,
			aggregateId,
			null,
			null,
			null,
			null,
			1,
			1
		);
		var result = await SearchAsync(query, cancellationToken);
		return result.Items.Count == 0 ? null : result.Items[0];
	}

	static async Task<List<AggregateSummaryResponse>> BuildAggregateRowsAsync(
		EventStoreDbContext context,
		SqlServerAdminTableDescriptor table,
		AggregateSearchQuery query,
		CancellationToken cancellationToken
	)
	{
		var aggregateTypeFilter = table.AggregateTypeFilter;
		var rows = context
			.EventStoreEntities.AsNoTracking()
			.Where(x =>
				x.EntityType == 0
				&& (aggregateTypeFilter == null || x.AggregateType == aggregateTypeFilter)
			);

		if (!string.IsNullOrWhiteSpace(query.AggregateId))
			rows = rows.Where(x => x.AggregateId == query.AggregateId);

		if (query.IsDeleted is not null)
			rows = rows.Where(x => x.IsDeleted == query.IsDeleted.Value);

		if (query.IsRestored == true)
			rows = rows.Where(x => !x.IsDeleted);

		if (query.FromUtc is not null)
			rows = rows.Where(x => x.Timestamp >= query.FromUtc.Value);

		if (query.ToUtc is not null)
			rows = rows.Where(x => x.Timestamp <= query.ToUtc.Value);

		return await rows.Select(x => new AggregateSummaryResponse(
				x.AggregateType,
				x.AggregateId,
				x.Version,
				x.Timestamp,
				x.Timestamp,
				x.IsDeleted,
				!x.IsDeleted
			))
			.ToListAsync(cancellationToken);
	}

	static IEnumerable<AggregateSummaryResponse> ApplySort(
		IEnumerable<AggregateSummaryResponse> rows,
		string sort
	)
	{
		var descending = sort.Contains("desc", StringComparison.OrdinalIgnoreCase);

		return sort switch
		{
			"AggregateId asc" => rows.OrderBy(x => x.AggregateId),
			"AggregateId desc" => rows.OrderByDescending(x => x.AggregateId),
			"CurrentVersion asc" => rows.OrderBy(x => x.CurrentVersion),
			"CurrentVersion desc" => rows.OrderByDescending(x => x.CurrentVersion),
			"CreatedUtc asc" => descending
				? rows.OrderByDescending(x => x.CreatedUtc)
				: rows.OrderBy(x => x.CreatedUtc),
			"CreatedUtc desc" => rows.OrderByDescending(x => x.CreatedUtc),
			_ => descending
				? rows.OrderByDescending(x => x.LastUpdatedUtc).ThenByDescending(x => x.AggregateId)
				: rows.OrderBy(x => x.LastUpdatedUtc).ThenBy(x => x.AggregateId),
		};
	}

	static EventStoreDbContext CreateContext(
		SqlServerEventStoreOptions options,
		SqlServerAdminTableDescriptor table
	)
	{
		var builder = new DbContextOptionsBuilder<EventStoreDbContext>();
		builder.UseSqlServer(options.ConnectionString);
		return new EventStoreDbContext(builder.Options, table.SchemaName, table.TableName);
	}
}
