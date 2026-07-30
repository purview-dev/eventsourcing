using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Admin.Abstractions;
using Purview.EventSourcing.Admin.Security;
using TUnit.Core;

namespace Purview.EventSourcing.Admin.UnitTests.Security;

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
		var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(ClaimTypes.Role, "Admin")
		}));

		// Act
		var permissions = await provider.GetPermissionsAsync(user, CancellationToken.None);

		// Assert
		await Assert.That(permissions).IsEmpty();
	}

	[Test]
	public void ServiceCollection_RegistersSecurity_WithDefaults()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddPurviewEventSourcingAdminSecurity();

		// Assert
		var provider = services.BuildServiceProvider();
		var permissionProvider = provider.GetRequiredService<IAdminPermissionProvider>();
		// Verify instance type
		if (permissionProvider is not DenyAllPermissionProvider)
			throw new InvalidOperationException("Expected DenyAllPermissionProvider");
	}

	[Test]
	public void ServiceCollection_RegistersSecurity_WithCustomProvider()
	{
		// Arrange
		var services = new ServiceCollection();
		var customProvider = new TestCustomPermissionProvider();

		// Act
		services.AddPurviewEventSourcingAdminSecurity(customProvider);

		// Assert
		var provider = services.BuildServiceProvider();
		var permissionProvider = provider.GetRequiredService<IAdminPermissionProvider>();
		if (!ReferenceEquals(permissionProvider, customProvider))
			throw new InvalidOperationException("Expected the same instance");
	}

	sealed class TestCustomPermissionProvider : IAdminPermissionProvider
	{
		public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
			ClaimsPrincipal user,
			CancellationToken cancellationToken)
		{
			return Task.FromResult<IReadOnlyList<AdminPermission>>(new[]
			{
				new AdminPermission(AdminFeature.SearchAggregates, null, true)
			});
		}
	}
}
