using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Admin.Abstractions.Services;

namespace Purview.EventSourcing.Admin.AzureStorage;

/// <summary>
/// Registers the Azure Storage-backed Admin query and projection services.
/// </summary>
public static class AdminAzureStorageServiceCollectionExtensions
{
	/// <summary>
	/// Adds transient <see cref="IAdminAggregateQueryService"/>, <see cref="IAdminEventQueryService"/> and
	/// <see cref="IAdminProjectionService"/> registrations backed by Azure Table Storage.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The configured service collection, allowing further chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
	public static IServiceCollection AddPurviewEventSourcingAdminAzureStorage(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddTransient<IAdminAggregateQueryService, AzureStorageAdminAggregateQueryService>();
		services.TryAddTransient<IAdminEventQueryService, AzureStorageAdminEventQueryService>();
		services.TryAddTransient<IAdminProjectionService, AzureStorageAdminProjectionService>();

		return services;
	}
}
