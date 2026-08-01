using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Admin.Abstractions;

namespace Purview.EventSourcing.Admin.Postgres;

public static class AdminPostgresServiceCollectionExtensions
{
	public static IServiceCollection AddPurviewEventSourcingAdminPostgres(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddTransient<IAdminAggregateQueryService, PostgresAdminAggregateQueryService>();
		services.TryAddTransient<IAdminEventQueryService, PostgresAdminEventQueryService>();
		services.TryAddTransient<IAdminProjectionService, PostgresAdminProjectionService>();

		return services;
	}
}
