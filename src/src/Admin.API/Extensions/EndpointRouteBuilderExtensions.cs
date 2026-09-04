using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Admin.API.Endpoints;

namespace Purview.EventSourcing.Admin.API;

/// <summary>
/// Maps the Admin portal minimal API endpoints onto the application's route table.
/// </summary>
public static class AdminApiEndpointRouteBuilderExtensions
{
	/// <summary>
	/// Maps the Admin portal endpoints, honouring the enabled feature toggles in <see cref="AdminPortalOptions"/>.
	/// </summary>
	/// <param name="app">The <see cref="WebApplication"/> to map the endpoints onto.</param>
	/// <param name="optionsAccessor">
	/// The options to configure the Admin portal with, or <see langword="null"/> to resolve them from the application's services.
	/// </param>
	/// <remarks>
	/// <para>
	/// When <see cref="AdminPortalOptions.Enabled"/> is <see langword="false"/> no endpoints are mapped. All mapped
	/// endpoints require authorization and are grouped under <see cref="AdminPortalOptions.RoutePrefix"/>.
	/// </para>
	/// </remarks>
	public static void MapPurviewEventSourcingAdminAPI(
		[NotNull] this WebApplication app,
		IOptions<AdminPortalOptions>? optionsAccessor = null
	)
	{
		optionsAccessor ??= app.Services.GetRequiredService<IOptions<AdminPortalOptions>>();
		var options = optionsAccessor.Value;

		if (!options.Enabled)
			return;

		var group = app.MapGroup(options.RoutePrefix).WithName("AdminPortal").RequireAuthorization();

		// Map endpoint groups
		if (options.Features.SearchAggregates)
			AdminAggregatesEndpoints.MapSearchAggregates(group);

		if (options.Features.ViewAggregate)
			AdminAggregatesEndpoints.MapViewAggregate(group);

		if (options.Features.ViewEvents)
		{
			AdminAggregatesEndpoints.MapEventRange(group);

			if (options.Features.ExportEvents)
				AdminAggregatesEndpoints.MapExportEvents(group);
		}

		if (options.Features.ProjectPointInTime)
		{
			AdminAggregatesEndpoints.MapProjectionAtVersion(group);
			AdminAggregatesEndpoints.MapProjectionAtTime(group);
		}
	}
}
