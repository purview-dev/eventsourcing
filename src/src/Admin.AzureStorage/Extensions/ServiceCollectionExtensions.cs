using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Admin.Abstractions.Services;

namespace Purview.EventSourcing.Admin.AzureStorage;

public static class AdminAzureStorageServiceCollectionExtensions
{
	public static IServiceCollection AddPurviewEventSourcingAdminAzureStorage(
		this IServiceCollection services
	)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddTransient<
			IAdminAggregateQueryService,
			AzureStorageAdminAggregateQueryService
		>();
		services.TryAddTransient<IAdminEventQueryService, AzureStorageAdminEventQueryService>();
		services.TryAddTransient<IAdminProjectionService, AzureStorageAdminProjectionService>();

		return services;
	}
}
