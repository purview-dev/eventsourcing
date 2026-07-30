using System.Security.Claims;

namespace Purview.EventSourcing.Admin.Abstractions;

/// <summary>
/// Pluggable permission provider for admin portal.
/// Deny-by-default: no matching allow = deny.
/// </summary>
public interface IAdminPermissionProvider
{
	Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
		ClaimsPrincipal user,
		CancellationToken cancellationToken);
}
