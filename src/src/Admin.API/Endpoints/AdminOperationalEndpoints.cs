using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Security;

namespace Purview.EventSourcing.Admin.API.Endpoints;

/// <summary>
/// Maps the Admin portal operational endpoints (capability contract and health summary).
/// These are read-only, opt-in, and separately authorized.
/// </summary>
public static class AdminOperationalEndpoints
{
	const string AdminPortalTag = "AdminPortal";

	/// <summary>
	/// Maps the <c>GET /capabilities</c> endpoint that reports the merged event-store capability
	/// contract for the registered providers.
	/// </summary>
	/// <param name="group">The route group to map the endpoint onto.</param>
	/// <param name="policyName">The host authorization policy required by the endpoint.</param>
	public static RouteHandlerBuilder MapCapabilities(
		RouteGroupBuilder group,
		string policyName = AdminPortalPolicies.ViewCapabilities
	)
	{
		return group
			.MapGet("/capabilities", GetCapabilitiesAsync)
			.WithName("GetCapabilities")
			.WithSummary("Get event store capabilities")
			.WithDescription("Returns the merged event-store capability contract for the registered providers.")
			.WithTags(AdminPortalTag)
			.Produces<EventStoreCapabilities>()
			.RequireAuthorization(policyName);
	}

	/// <summary>
	/// Maps the <c>GET /health</c> endpoint that reports operational readiness for the Admin portal.
	/// Readiness reflects whether the event-store capability contract can be resolved; it does not
	/// probe live storage.
	/// </summary>
	/// <param name="group">The route group to map the endpoint onto.</param>
	/// <param name="policyName">The host authorization policy required by the endpoint.</param>
	public static RouteHandlerBuilder MapHealth(
		RouteGroupBuilder group,
		string policyName = AdminPortalPolicies.ViewCapabilities
	)
	{
		return group
			.MapGet("/health", GetHealthAsync)
			.WithName("GetAdminHealth")
			.WithSummary("Get Admin portal health")
			.WithDescription("Reports whether the Admin portal can resolve the event-store capability contract.")
			.WithTags(AdminPortalTag)
			.Produces<AdminHealthResponse>()
			.RequireAuthorization(policyName);
	}

	static async Task<Ok<EventStoreCapabilities>> GetCapabilitiesAsync(
		IEventStoreCapabilitiesProvider capabilitiesProvider,
		IAdminAuditLogger auditLogger,
		HttpContext httpContext,
		CancellationToken cancellationToken
	)
	{
		var capabilities = capabilitiesProvider.GetCapabilities();

		await auditLogger.LogAsync(
			new AdminAuditEntry(
				DateTimeOffset.UtcNow,
				AdminFeature.ViewCapabilities,
				"Read",
				Principal(httpContext),
				Target: null,
				Succeeded: true,
				Details: null
			),
			cancellationToken
		);

		return TypedResults.Ok(capabilities);
	}

	static async Task<Ok<AdminHealthResponse>> GetHealthAsync(
		IEventStoreCapabilitiesProvider capabilitiesProvider,
		IAdminAuditLogger auditLogger,
		HttpContext httpContext,
		CancellationToken cancellationToken
	)
	{
		var capabilities = capabilitiesProvider.GetCapabilities();

		await auditLogger.LogAsync(
			new AdminAuditEntry(
				DateTimeOffset.UtcNow,
				AdminFeature.ViewCapabilities,
				"Read",
				Principal(httpContext),
				Target: "health",
				Succeeded: true,
				Details: null
			),
			cancellationToken
		);

		return TypedResults.Ok(
			new AdminHealthResponse(
				Status: "Ready",
				TimestampUtc: DateTimeOffset.UtcNow,
				TransactionGuarantee: capabilities.TransactionGuarantee,
				SupportsEventStreams: capabilities.SupportsEventStreams,
				SupportsQueries: capabilities.SupportsQueries,
				SupportsTransactionalOutbox: capabilities.SupportsTransactionalOutbox,
				OperationalLimitations: capabilities.OperationalLimitations
			)
		);
	}

	static string? Principal(HttpContext httpContext) => httpContext.User.Identity?.Name;
}
