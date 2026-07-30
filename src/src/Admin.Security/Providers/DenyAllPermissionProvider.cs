using System.Security.Claims;
using Purview.EventSourcing.Admin.Abstractions;

namespace Purview.EventSourcing.Admin.Security;

/// <summary>
/// Default deny-by-default permission provider.
/// Returns empty list (all denied) unless explicitly overridden.
/// </summary>
public sealed class DenyAllPermissionProvider : IAdminPermissionProvider
{
	public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
		ClaimsPrincipal user,
		CancellationToken cancellationToken)
	{
		return Task.FromResult<IReadOnlyList<AdminPermission>>(Array.Empty<AdminPermission>());
	}
}
