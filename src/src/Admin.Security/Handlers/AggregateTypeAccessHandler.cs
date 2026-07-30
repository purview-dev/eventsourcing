using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Purview.EventSourcing.Admin.Abstractions;

namespace Purview.EventSourcing.Admin.Security.Handlers;

public sealed class AggregateTypeAccessHandler(IAdminPermissionProvider permissionProvider)
	: AuthorizationHandler<AggregateTypeAccessRequirement>
{
	protected override async Task HandleRequirementAsync(
		[NotNull] AuthorizationHandlerContext context,
		AggregateTypeAccessRequirement requirement
	)
	{
		// This handler delegates the actual aggregate type checking to endpoint-level logic
		// which has access to route parameters. For now, we just check that general
		// aggregate access is allowed, and route-specific checks happen in endpoint filters.
		var permissions = await permissionProvider.GetPermissionsAsync(context.User, CancellationToken.None);

		// If no explicit aggregate-type-scoped denial exists, allow (the endpoint filter will do fine-grained checks)
		var hasSomePermission = permissions.Any(p => p.Allowed);

		if (hasSomePermission || permissions.Count == 0)
		{
			// Either user has some permission, or we use deny-by-default at endpoint level
			context.Succeed(requirement);
		}
		else
		{
			context.Fail();
		}
	}
}
