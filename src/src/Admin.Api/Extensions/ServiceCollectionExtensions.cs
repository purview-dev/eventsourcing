using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Admin.Api;

/// <summary>
/// Registers the Admin API options and validation services.
/// </summary>
public static class AdminApiServiceCollectionExtensions
{
	/// <summary>
	/// Adds the Admin API option bindings and validation hooks to the service collection.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="configure">Optional in-memory configuration delegate.</param>
	/// <returns>The configured service collection.</returns>
	public static IServiceCollection AddPurviewEventSourcingAdminApi(
		this IServiceCollection services,
		Action<AdminPortalOptions>? configure = null
	)
	{
		services.AddOptions<AdminPortalOptions>().Configure(options => configure?.Invoke(options)).ValidateOnStart();

		services.AddSingleton<IValidateOptions<AdminPortalOptions>>(new AdminPortalOptionsValidator());

		return services;
	}
}

sealed class AdminPortalOptionsValidator : IValidateOptions<AdminPortalOptions>
{
	public ValidateOptionsResult Validate(string? name, AdminPortalOptions options)
	{
		try
		{
			options.Validate();
			return ValidateOptionsResult.Success;
		}
		catch (InvalidOperationException ex)
		{
			return ValidateOptionsResult.Fail(ex.Message);
		}
		catch (ArgumentException ex)
		{
			return ValidateOptionsResult.Fail(ex.Message);
		}
	}
}
