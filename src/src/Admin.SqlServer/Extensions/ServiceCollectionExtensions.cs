using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.SqlServer.Events;

namespace Purview.EventSourcing.Admin.SqlServer;

public static class AdminSqlServerServiceCollectionExtensions
{
	public static IServiceCollection AddPurviewEventSourcingAdminSqlServer(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddTransient<IAdminAggregateQueryService, SqlServerAdminAggregateQueryService>();
		services.TryAddTransient<IAdminEventQueryService, SqlServerAdminEventQueryService>();
		services.TryAddTransient<IAdminProjectionService, SqlServerAdminProjectionService>();

		return services;
	}
}
