using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Security;
using Purview.EventSourcing.Outbox;

namespace Purview.EventSourcing.Admin.API;

public sealed class AdminOperationalEndpointsTests
{
	[Test]
	public async Task Capabilities_WhenFeatureEnabled_ReturnsCapabilityContract(CancellationToken cancellationToken)
	{
		await using var host = await AdminTestHost.CreateAsync(configureAdmin: static options =>
			options.Features.ViewCapabilities = true
		);
		var client = host.Client;

		var response = await client.GetAsync("/admin/api/capabilities", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
		var root = json!.RootElement;
		await Assert.That(root.GetProperty("transactionGuarantee").GetInt32()).IsEqualTo(1);
		await Assert.That(root.GetProperty("supportsEventStreams").GetBoolean()).IsTrue();
		await Assert.That(root.GetProperty("supportsQueries").GetBoolean()).IsTrue();
	}

	[Test]
	public async Task Capabilities_WhenFeatureDisabled_ReturnsNotFound(CancellationToken cancellationToken)
	{
		await using var host = await AdminTestHost.CreateAsync();
		var client = host.Client;

		var response = await client.GetAsync("/admin/api/capabilities", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task Health_WhenFeatureEnabled_ReturnsReadinessSummary(CancellationToken cancellationToken)
	{
		await using var host = await AdminTestHost.CreateAsync(configureAdmin: static options =>
			options.Features.ViewCapabilities = true
		);
		var client = host.Client;

		var response = await client.GetAsync("/admin/api/health", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
		var root = json!.RootElement;
		await Assert.That(root.GetProperty("status").GetString()).IsEqualTo("Ready");
		await Assert.That(root.GetProperty("supportsTransactionalOutbox").GetBoolean()).IsFalse();
	}

	[Test]
	public async Task Capabilities_GivenDeniedPermission_ReturnsForbidden(CancellationToken cancellationToken)
	{
		await using var host = await AdminTestHost.CreateAsync(
			configureAdmin: static options => options.Features.ViewCapabilities = true,
			permissionProvider: new DenyCapabilitiesPermissionProvider()
		);
		var client = host.Client;

		var response = await client.GetAsync("/admin/api/capabilities", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task Capabilities_GivenEnabledFeature_AuditsPrivilegedRead(CancellationToken cancellationToken)
	{
		await using var host = await AdminTestHost.CreateAsync(configureAdmin: static options =>
			options.Features.ViewCapabilities = true
		);
		var client = host.Client;

		await client.GetAsync("/admin/api/capabilities", cancellationToken);
		await client.GetAsync("/admin/api/health", cancellationToken);

		var auditLogger = host.App.Services.GetRequiredService<IAdminAuditLogger>();
		var audit = (InMemoryAdminAuditLogger)auditLogger;
		await Assert.That(audit.Entries.Count).IsEqualTo(2);
		await Assert.That(audit.Entries.All(static entry => entry.Feature == AdminFeature.ViewCapabilities)).IsTrue();
		await Assert.That(audit.Entries.All(static entry => entry.Succeeded)).IsTrue();
	}

	[Test]
	public async Task PoisonedOutbox_WhenEnabledAndStoreRegistered_ReturnsPoisonedMessages(
		CancellationToken cancellationToken
	)
	{
		await using var host = await AdminTestHost.CreateAsync(
			configureAdmin: static options => options.Features.ViewPoisonedOutbox = true,
			configureServices: static services => services.AddSingleton<IOutboxStore>(new StubPoisonedOutboxStore())
		);
		var client = host.Client;

		var response = await client.GetAsync("/admin/api/outbox/poisoned", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
		var root = json!.RootElement;
		await Assert.That(root.GetProperty("count").GetInt32()).IsEqualTo(1);
		var item = root.GetProperty("items")[0];
		await Assert.That(item.GetProperty("eventType").GetString()).IsEqualTo("OrderCreated");
		await Assert.That(item.GetProperty("lastError").GetString()).IsNotEmpty();
	}

	[Test]
	public async Task PoisonedOutbox_WhenNoOutboxStoreRegistered_ReturnsNotFound(CancellationToken cancellationToken)
	{
		await using var host = await AdminTestHost.CreateAsync(configureAdmin: static options =>
			options.Features.ViewPoisonedOutbox = true
		);
		var client = host.Client;

		var response = await client.GetAsync("/admin/api/outbox/poisoned", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task PoisonedOutbox_WhenFeatureDisabled_ReturnsNotFound(CancellationToken cancellationToken)
	{
		await using var host = await AdminTestHost.CreateAsync();
		var client = host.Client;

		var response = await client.GetAsync("/admin/api/outbox/poisoned", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task PoisonedOutbox_GivenDeniedPermission_ReturnsForbidden(CancellationToken cancellationToken)
	{
		await using var host = await AdminTestHost.CreateAsync(
			configureAdmin: static options => options.Features.ViewPoisonedOutbox = true,
			permissionProvider: new DenyPoisonedOutboxPermissionProvider()
		);
		var client = host.Client;

		var response = await client.GetAsync("/admin/api/outbox/poisoned", cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}

	sealed class DenyCapabilitiesPermissionProvider : IAdminPermissionProvider
	{
		public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
			System.Security.Claims.ClaimsPrincipal user,
			CancellationToken cancellationToken
		) =>
			Task.FromResult<IReadOnlyList<AdminPermission>>([new(AdminFeature.ViewCapabilities, null, Allowed: false)]);
	}

	sealed class DenyPoisonedOutboxPermissionProvider : IAdminPermissionProvider
	{
		public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
			System.Security.Claims.ClaimsPrincipal user,
			CancellationToken cancellationToken
		) =>
			Task.FromResult<IReadOnlyList<AdminPermission>>([
				new(AdminFeature.ViewPoisonedOutbox, null, Allowed: false),
			]);
	}

	sealed class StubPoisonedOutboxStore : IOutboxStore
	{
		public Task<int> EnqueueAsync(OutboxEnvelope message, CancellationToken cancellationToken) =>
			throw new NotSupportedException();

		public Task<IReadOnlyList<OutboxEnvelope>> ClaimNextBatchAsync(
			string leaseOwner,
			DateTimeOffset leaseUntil,
			int batchSize,
			CancellationToken cancellationToken
		) => throw new NotSupportedException();

		public Task CompleteAsync(string id, CancellationToken cancellationToken) => throw new NotSupportedException();

		public Task MarkFailedAsync(
			string id,
			string errorMessage,
			DateTimeOffset nextAttemptUtc,
			CancellationToken cancellationToken
		) => throw new NotSupportedException();

		public Task MarkPoisonedAsync(string id, string errorMessage, CancellationToken cancellationToken) =>
			throw new NotSupportedException();

		public Task<int> CleanupAsync(TimeSpan retention, CancellationToken cancellationToken) =>
			throw new NotSupportedException();

		public Task<IReadOnlyList<OutboxEnvelope>> GetPoisonedAsync(
			int skip,
			int take,
			CancellationToken cancellationToken
		) =>
			Task.FromResult<IReadOnlyList<OutboxEnvelope>>([
				new OutboxEnvelope(
					"poison-1",
					"OrderAggregate",
					"order-1",
					"OrderCreated",
					"{}",
					IdempotencyKey: null,
					CorrelationId: null,
					CreatedUtc: DateTimeOffset.UtcNow
				)
				{
					State = OutboxState.Poisoned,
					AttemptCount = 5,
					LastError = "handler failed",
				},
			]);
	}
}
