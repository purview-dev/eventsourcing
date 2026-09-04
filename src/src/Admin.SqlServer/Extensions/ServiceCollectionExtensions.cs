using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Admin.Abstractions.Services;

namespace Purview.EventSourcing.Admin.SqlServer;

/// <summary>
/// Registers the SQL Server-backed Admin query and projection services.
/// </summary>
public static class AdminSqlServerServiceCollectionExtensions
{
	/// <summary>
	/// Adds transient <see cref="IAdminAggregateQueryService"/>, <see cref="IAdminEventQueryService"/> and
	/// <see cref="IAdminProjectionService"/> registrations backed by SQL Server.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The configured service collection, allowing further chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
	public static IServiceCollection AddPurviewEventSourcingAdminSqlServer(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddTransient<IAdminAggregateQueryService, SqlServerAdminAggregateQueryService>();
		services.TryAddTransient<IAdminEventQueryService, SqlServerAdminEventQueryService>();
		services.TryAddTransient<IAdminProjectionService, SqlServerAdminProjectionService>();

		return services;
	}
}
