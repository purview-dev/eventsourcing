using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Security.Requirements;

namespace Purview.EventSourcing.Admin.Security.Handlers;

public sealed class AdminFeatureAuthorizationHandler(IAdminPermissionProvider permissionProvider)
	: AuthorizationHandler<AdminFeatureRequirement>
{
	protected override async Task HandleRequirementAsync(
		[NotNull] AuthorizationHandlerContext context,
		AdminFeatureRequirement requirement
	)
	{
		var permissions = await permissionProvider.GetPermissionsAsync(
			context.User,
			CancellationToken.None
		);

		var hasPermission =
			permissions.FirstOrDefault(p =>
				p.Feature == requirement.Feature && p.Allowed && (p.AggregateType == null)
			)
			is not null;

		if (hasPermission)
			context.Succeed(requirement);
		else
			context.Fail();
	}
}
