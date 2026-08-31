using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Admin.Api.Contracts;
using ZodSharp.AspNetCore;

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

		// Register the ZodSharp schema factory and auto-discover the generated validators for the Admin API
		// request contracts and options so endpoint filters and option validation can resolve them by type.
		services.AddZodSharp(options =>
		{
			options.ScanAssemblies.Add(typeof(EventRangeRequest).Assembly);
		});

		return services;
	}
}

/// <summary>
/// Validates <see cref="AdminPortalOptions"/> using the source-generated
/// <see cref="AdminPortalOptionsSchema"/> schema and the bespoke cross-field rules in
/// <see cref="AdminPortalOptions.Validate"/>.
/// </summary>
sealed class AdminPortalOptionsValidator : IValidateOptions<AdminPortalOptions>
{
	public ValidateOptionsResult Validate(string? name, AdminPortalOptions options)
	{
		var schemaResult = AdminPortalOptionsSchema.Validate(options);
		if (!schemaResult.IsSuccess)
		{
			return ValidateOptionsResult.Fail(schemaResult.Errors.Select(error => error.Message));
		}

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
