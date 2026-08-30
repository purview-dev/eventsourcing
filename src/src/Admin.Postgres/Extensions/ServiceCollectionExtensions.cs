using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Admin.Abstractions.Services;

namespace Purview.EventSourcing.Admin.Postgres;

/// <summary>
/// Registers the PostgreSQL-backed Admin query and projection services.
/// </summary>
public static class AdminPostgresServiceCollectionExtensions
{
	/// <summary>
	/// Adds transient <see cref="IAdminAggregateQueryService"/>, <see cref="IAdminEventQueryService"/> and
	/// <see cref="IAdminProjectionService"/> registrations backed by PostgreSQL.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The configured service collection, allowing further chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
	public static IServiceCollection AddPurviewEventSourcingAdminPostgres(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddTransient<IAdminAggregateQueryService, PostgresAdminAggregateQueryService>();
		services.TryAddTransient<IAdminEventQueryService, PostgresAdminEventQueryService>();
		services.TryAddTransient<IAdminProjectionService, PostgresAdminProjectionService>();

		return services;
	}
}
