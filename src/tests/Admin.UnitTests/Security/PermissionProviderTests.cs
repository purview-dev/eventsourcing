using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Security.Providers;

namespace Purview.EventSourcing.Admin.Security;

public sealed class PermissionProviderTests
{
	[Test]
	public async Task DenyAllPermissionProvider_ReturnsEmptyList_ForAnyUser()
	{
		// Arrange
		var provider = new DenyAllPermissionProvider();
		var user = new ClaimsPrincipal();

		// Act
		var permissions = await provider.GetPermissionsAsync(user, CancellationToken.None);

		// Assert
		await Assert.That(permissions).IsEmpty();
	}

	[Test]
	public async Task DenyAllPermissionProvider_DeniesAllByDefault()
	{
		// Arrange
		var provider = new DenyAllPermissionProvider();
		var user = new ClaimsPrincipal(
			new ClaimsIdentity([
				new Claim(ClaimTypes.NameIdentifier, "user123"),
				new Claim(ClaimTypes.Role, "Admin"),
			])
		);

		// Act
		var permissions = await provider.GetPermissionsAsync(user, CancellationToken.None);

		// Assert
		await Assert.That(permissions).IsEmpty();
	}

	[Test]
	public async Task ServiceCollection_RegistersSecurity_WithDefaults()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddPurviewEventSourcingAdminSecurity();
		var provider = services.BuildServiceProvider();
		var permissionProvider = provider.GetRequiredService<IAdminPermissionProvider>();

		// Assert — verify instance type
		await Assert.That(permissionProvider).IsTypeOf<DenyAllPermissionProvider>();
	}

	[Test]
	public async Task ServiceCollection_RegistersSecurity_WithCustomProvider()
	{
		// Arrange
		var customProvider = new TestCustomPermissionProvider();
		var services = new ServiceCollection();

		// Act
		services.AddPurviewEventSourcingAdminSecurity(customProvider);
		var provider = services.BuildServiceProvider();
		var resolvedProvider = provider.GetRequiredService<IAdminPermissionProvider>();

		// Assert — verify exact same instance (singleton registration)
		await Assert.That(ReferenceEquals(resolvedProvider, customProvider)).IsTrue();
	}

	[Test]
	public async Task ServiceCollection_RegistersAuthorizationHandlers()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddAuthorizationBuilder();

		// Act
		services.AddPurviewEventSourcingAdminSecurity();
		var provider = services.BuildServiceProvider();

		// Assert — verify handlers are registered
		var handlers = provider.GetServices<IAuthorizationHandler>();
		await Assert.That(handlers).Count().IsGreaterThanOrEqualTo(2);
	}

	sealed class TestCustomPermissionProvider : IAdminPermissionProvider
	{
		public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
			ClaimsPrincipal user,
			CancellationToken cancellationToken
		)
		{
			return Task.FromResult<IReadOnlyList<AdminPermission>>([
				new AdminPermission(AdminFeature.SearchAggregates, null, true),
			]);
		}
	}
}
