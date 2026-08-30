using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Purview.EventSourcing.Admin.Site;

/// <summary>
/// Registers the admin portal Razor Pages UI and maps its routes.
/// </summary>
public static class AdminSiteServiceCollectionExtensions
{
	/// <summary>
	/// Adds the admin portal Razor Pages to the MVC application.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="enableRazorRuntimeCompilation">
	/// When <see langword="true"/>, enables Razor runtime compilation so page markup can be edited without rebuilding.
	/// </param>
	/// <returns>The configured MVC builder for chaining.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
	public static IMvcBuilder AddPurviewEventSourcingAdminSite(
		this IServiceCollection services,
		bool enableRazorRuntimeCompilation = false
	)
	{
		ArgumentNullException.ThrowIfNull(services);
		var mvcBuilder = services.AddRazorPages();
		if (enableRazorRuntimeCompilation)
			mvcBuilder.AddRazorRuntimeCompilation();

		return mvcBuilder;
	}

	/// <summary>
	/// Maps the admin portal Razor Pages onto the application's route table.
	/// </summary>
	/// <param name="app">The <see cref="WebApplication"/> to map the pages onto.</param>
	/// <param name="pathPrefix">The route prefix for the admin pages. Defaults to <c>/admin</c>.</param>
	/// <returns>The application for chaining.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException"><paramref name="pathPrefix"/> is <see langword="null"/> or whitespace.</exception>
	public static WebApplication MapPurviewEventSourcingAdminSite(this WebApplication app, string pathPrefix = "/admin")
	{
		ArgumentNullException.ThrowIfNull(app);
		ArgumentException.ThrowIfNullOrWhiteSpace(pathPrefix);

		app.MapRazorPages();

		return app;
	}
}
