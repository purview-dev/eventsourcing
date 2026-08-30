using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Queries;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Api.Contracts;
using Purview.EventSourcing.Admin.Security;

namespace Purview.EventSourcing.Admin.Api.Endpoints;

/// <summary>
/// Maps the Admin portal aggregate query endpoints onto a route group.
/// </summary>
public static class AdminAggregatesEndpoints
{
	/// <summary>
	/// Maps the <c>POST /aggregates/search</c> endpoint used to search for aggregates.
	/// </summary>
	/// <param name="group">The route group to map the endpoint onto.</param>
	public static void MapSearchAggregates(RouteGroupBuilder group)
	{
		group
			.MapPost("/aggregates/search", SearchAggregatesAsync)
			.WithName("SearchAggregates")
			.WithSummary("Search for aggregates")
			.WithDescription("Search aggregates by type, id, date range, or status flags with pagination support.")
			.RequireAuthorization(AdminPortalPolicies.SearchAggregates);
	}

	/// <summary>
	/// Maps the <c>GET /aggregates/{aggregateType}/{aggregateId}/events</c> endpoint used to read an aggregate's event stream.
	/// </summary>
	/// <param name="group">The route group to map the endpoint onto.</param>
	public static void MapEventRange(RouteGroupBuilder group)
	{
		group
			.MapGet("/aggregates/{aggregateType}/{aggregateId}/events", GetEventRangeAsync)
			.WithName("GetAggregateEventRange")
			.WithSummary("Get aggregate event range")
			.WithDescription("Returns the aggregate event stream within version and timestamp bounds.")
			.RequireAuthorization(AdminPortalPolicies.ViewEvents);
	}

	/// <summary>
	/// Maps the <c>GET /aggregates/{aggregateType}/{aggregateId}/projection</c> endpoint used to project state at a version.
	/// </summary>
	/// <param name="group">The route group to map the endpoint onto.</param>
	public static void MapProjectionAtVersion(RouteGroupBuilder group)
	{
		group
			.MapGet("/aggregates/{aggregateType}/{aggregateId}/projection", GetProjectionAtVersionAsync)
			.WithName("GetAggregateProjectionAtVersion")
			.WithSummary("Get aggregate projection at version")
			.WithDescription("Projects aggregate state at a specific version.")
			.RequireAuthorization(AdminPortalPolicies.ProjectPointInTime);
	}

	/// <summary>
	/// Maps the <c>GET /aggregates/{aggregateType}/{aggregateId}/projection/time</c> endpoint used to project state at a timestamp.
	/// </summary>
	/// <param name="group">The route group to map the endpoint onto.</param>
	public static void MapProjectionAtTime(RouteGroupBuilder group)
	{
		group
			.MapGet("/aggregates/{aggregateType}/{aggregateId}/projection/time", GetProjectionAtTimeAsync)
			.WithName("GetAggregateProjectionAtTime")
			.WithSummary("Get aggregate projection at time")
			.WithDescription("Projects aggregate state at a specific UTC timestamp.")
			.RequireAuthorization(AdminPortalPolicies.ProjectPointInTime);
	}

	static async Task<Ok<PagedResult<AggregateSummaryResponse>>> SearchAggregatesAsync(
		AggregateSearchRequest request,
		IAdminAggregateQueryService queryService,
		IOptions<AdminPortalOptions> options,
		CancellationToken cancellationToken
	)
	{
		// Clamp page size to max
		var pageSize = Math.Min(request.PageSize, options.Value.Paging.MaxPageSize);
		pageSize = Math.Max(pageSize, 1);

		var query = new AggregateSearchQuery(
			request.AggregateType,
			request.AggregateId,
			request.FromUtc,
			request.ToUtc,
			request.IsDeleted,
			request.IsRestored,
			request.Page,
			pageSize,
			request.Sort
		);

		var result = await queryService.SearchAsync(query, cancellationToken);
		return TypedResults.Ok(result);
	}

	static async Task<Results<Ok<PagedResult<EventEnvelopeResponse>>, NotFound>> GetEventRangeAsync(
		string aggregateType,
		string aggregateId,
		[AsParameters] EventRangeRequest request,
		IAdminEventQueryService queryService,
		CancellationToken cancellationToken
	)
	{
		var query = new EventRangeQuery(
			request.VersionFrom,
			request.VersionTo,
			request.TimeFromUtc,
			request.TimeToUtc,
			request.Page,
			request.PageSize,
			request.Sort
		);

		var result = await queryService.GetRangeAsync(aggregateType, aggregateId, query, cancellationToken);
		return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
	}

	static async Task<Results<Ok<ProjectionResponse>, NotFound>> GetProjectionAtVersionAsync(
		string aggregateType,
		string aggregateId,
		[FromQuery] long? version,
		IAdminProjectionService projectionService,
		CancellationToken cancellationToken
	)
	{
		if (version is null or <= 0)
			return TypedResults.NotFound();

		var result = await projectionService.ProjectAtVersionAsync(
			aggregateType,
			aggregateId,
			version.Value,
			cancellationToken
		);

		return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
	}

	static async Task<Results<Ok<ProjectionResponse>, NotFound>> GetProjectionAtTimeAsync(
		string aggregateType,
		string aggregateId,
		[FromQuery] DateTime? asOfUtc,
		IAdminProjectionService projectionService,
		CancellationToken cancellationToken
	)
	{
		if (asOfUtc is null)
			return TypedResults.NotFound();

		var result = await projectionService.ProjectAtTimeAsync(
			aggregateType,
			aggregateId,
			new DateTimeOffset(asOfUtc.Value, TimeSpan.Zero),
			cancellationToken
		);

		return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
	}
}
