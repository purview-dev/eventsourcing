using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Security;

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

	sealed class DenyCapabilitiesPermissionProvider : IAdminPermissionProvider
	{
		public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
			System.Security.Claims.ClaimsPrincipal user,
			CancellationToken cancellationToken
		) =>
			Task.FromResult<IReadOnlyList<AdminPermission>>([new(AdminFeature.ViewCapabilities, null, Allowed: false)]);
	}
}
