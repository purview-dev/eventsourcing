using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Purview.EventSourcing.Admin.Site;

public static class AdminSiteServiceCollectionExtensions
{
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

	public static WebApplication MapPurviewEventSourcingAdminSite(this WebApplication app, string pathPrefix = "/admin")
	{
		ArgumentNullException.ThrowIfNull(app);
		ArgumentException.ThrowIfNullOrWhiteSpace(pathPrefix);

		app.MapRazorPages();

		return app;
	}
}
