using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Security;
using Purview.EventSourcing.Outbox;

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

	/// <summary>
	/// Maps the <c>GET /outbox/poisoned</c> endpoint that lists poisoned (dead-letter) transactional
	/// outbox messages.
	/// </summary>
	/// <param name="group">The route group to map the endpoint onto.</param>
	/// <param name="policyName">The host authorization policy required by the endpoint.</param>
	public static RouteHandlerBuilder MapPoisonedOutbox(
		RouteGroupBuilder group,
		string policyName = AdminPortalPolicies.ViewPoisonedOutbox
	)
	{
		return group
			.MapGet("/outbox/poisoned", GetPoisonedOutboxAsync)
			.WithName("GetPoisonedOutbox")
			.WithSummary("Get poisoned outbox messages")
			.WithDescription("Returns poisoned (dead-letter) transactional outbox messages.")
			.WithTags(AdminPortalTag)
			.Produces<PoisonedOutboxResponse>()
			.RequireAuthorization(policyName);
	}

	static async Task<Results<Ok<PoisonedOutboxResponse>, NotFound>> GetPoisonedOutboxAsync(
		IAdminAuditLogger auditLogger,
		HttpContext httpContext,
		CancellationToken cancellationToken
	)
	{
		var outboxStore = httpContext.RequestServices.GetService<IOutboxStore>();
		if (outboxStore is null)
			return TypedResults.NotFound();

		var messages = await outboxStore.GetPoisonedAsync(0, 100, cancellationToken);

		await auditLogger.LogAsync(
			new AdminAuditEntry(
				DateTimeOffset.UtcNow,
				AdminFeature.ViewPoisonedOutbox,
				"Read",
				Principal(httpContext),
				Target: "outbox/poisoned",
				Succeeded: true,
				Details: null
			),
			cancellationToken
		);

		return TypedResults.Ok(new PoisonedOutboxResponse(messages, messages.Count));
	}

	static string? Principal(HttpContext httpContext) => httpContext.User.Identity?.Name;
}

/// <summary>
/// A page of poisoned (dead-letter) transactional outbox messages.
/// </summary>
/// <param name="Items">The poisoned messages, most recently poisoned first.</param>
/// <param name="Count">The number of messages returned.</param>
public sealed record PoisonedOutboxResponse(IReadOnlyList<OutboxEnvelope> Items, int Count);
