using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Admin.Abstractions;
using Purview.EventSourcing.Admin.Security.Handlers;

namespace Purview.EventSourcing.Admin.Security;

public static class AdminSecurityServiceCollectionExtensions
{
	public static IServiceCollection AddPurviewEventSourcingAdminSecurity(
		this IServiceCollection services,
		IAdminPermissionProvider? permissionProvider = null
	)
	{
		// Deny-by-default if no provider supplied
		services.AddSingleton(permissionProvider ?? new DenyAllPermissionProvider());

		// Register authorization handlers
		services.AddScoped<IAuthorizationHandler, AdminFeatureAuthorizationHandler>();
		services.AddScoped<IAuthorizationHandler, AggregateTypeAccessHandler>();

		return services;
	}

	public static AuthorizationBuilder AddPurviewEventSourcingAdminPolicies([NotNull] this AuthorizationBuilder builder)
	{
		builder.AddPolicy(
			AdminPortalPolicies.SearchAggregates,
			policy => policy.AddRequirements(new AdminFeatureRequirement(AdminFeature.SearchAggregates))
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewAggregate,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ViewAggregate),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewEvents,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ViewEvents),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.ProjectPointInTime,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ProjectPointInTime),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.ExportEvents,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ExportEvents),
					new AggregateTypeAccessRequirement()
				)
		);

		return builder;
	}
}
