using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Security.Requirements;

namespace Purview.EventSourcing.Admin.Security.Handlers;

/// <summary>
/// Authorization handler that grants a requirement only when the user holds an explicit
/// allowed permission for the feature.
/// </summary>
/// <param name="permissionProvider">The permission provider used to resolve the current user's permissions.</param>
public sealed class AdminFeatureAuthorizationHandler(IAdminPermissionProvider permissionProvider)
	: AuthorizationHandler<AdminFeatureRequirement>
{
	///<inheritdoc/>
	protected override async Task HandleRequirementAsync(
		[NotNull] AuthorizationHandlerContext context,
		AdminFeatureRequirement requirement
	)
	{
		var permissions = await permissionProvider.GetPermissionsAsync(context.User, CancellationToken.None);

		var hasPermission =
			permissions.FirstOrDefault(p => p.Feature == requirement.Feature && p.Allowed && (p.AggregateType == null))
			is not null;

		if (hasPermission)
			context.Succeed(requirement);
		else
			context.Fail();
	}
}
