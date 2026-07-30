using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Admin.Api;

public static class AdminApiServiceCollectionExtensions
{
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
		catch (Exception ex)
		{
			return ValidateOptionsResult.Fail(ex.Message);
		}
	}
}
