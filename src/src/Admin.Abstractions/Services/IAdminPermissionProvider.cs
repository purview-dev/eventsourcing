using System.Security.Claims;
using Purview.EventSourcing.Admin.Abstractions.Models;

namespace Purview.EventSourcing.Admin.Abstractions.Services;

/// <summary>
/// Pluggable permission provider for admin portal.
/// Deny-by-default: no matching allow = deny.
/// </summary>
public interface IAdminPermissionProvider
{
	Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken);
}
