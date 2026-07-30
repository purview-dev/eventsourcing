using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Admin.Api;

public static class AdminApiEndpointRouteBuilderExtensions
{
	public static void MapPurviewEventSourcingAdminApi(
		this WebApplication app,
		IOptions<AdminPortalOptions>? optionsAccessor = null
	)
	{
		optionsAccessor ??= app.Services.GetRequiredService<IOptions<AdminPortalOptions>>();
		var options = optionsAccessor.Value;

		if (!options.Enabled)
			return;

		var group = app.MapGroup(options.RoutePrefix).WithName("AdminPortal").WithOpenApi().RequireAuthorization();

		// Map endpoint groups
		if (options.Features.SearchAggregates)
			AdminAggregatesEndpoints.MapSearchAggregates(group);

		if (options.Features.ViewEvents)
			AdminAggregatesEndpoints.MapEventRange(group);

		if (options.Features.ProjectPointInTime)
		{
			AdminAggregatesEndpoints.MapProjectionAtVersion(group);
			AdminAggregatesEndpoints.MapProjectionAtTime(group);
		}
	}
}
