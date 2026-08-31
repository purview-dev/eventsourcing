using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Admin.Client;

namespace Purview.EventSourcing.Admin.Site;

/// <summary>
/// Registers the admin portal Razor Pages UI, its generated Admin API client, and maps its routes.
/// </summary>
public static class AdminSiteServiceCollectionExtensions
{
	/// <summary>
	/// Adds the admin portal Razor Pages to the MVC application and registers the generated Admin API client used
	/// by the pages.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="enableRazorRuntimeCompilation">
	/// When <see langword="true"/>, enables Razor runtime compilation so page markup can be edited without rebuilding.
	/// </param>
	/// <param name="configureClient">Optional <see cref="AdminClientOptions"/> configuration.</param>
	/// <returns>The configured MVC builder for chaining.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
	public static IMvcBuilder AddPurviewEventSourcingAdminSite(
		this IServiceCollection services,
		bool enableRazorRuntimeCompilation = false,
		Action<AdminClientOptions>? configureClient = null
	)
	{
		ArgumentNullException.ThrowIfNull(services);
		var mvcBuilder = services.AddRazorPages();
		if (enableRazorRuntimeCompilation)
			mvcBuilder.AddRazorRuntimeCompilation();

		services.AddHttpContextAccessor();
		services.AddTransient<SameOriginResolverHandler>();
		services.AddAdminApiClient(
			configureClient,
			clientBuilder => clientBuilder.AddHttpMessageHandler<SameOriginResolverHandler>()
		);

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

/// <summary>
/// Resolves the origin of relative Admin API request URIs from the current HTTP request, allowing the pages to
/// call the Admin API in the same web application without a configured <see cref="AdminClientOptions.BaseUrl"/>.
/// </summary>
sealed class SameOriginResolverHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
	const string PlaceholderHost = "admin.invalid";

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken
	)
	{
		if (
			request.RequestUri is { } uri
			&& (!uri.IsAbsoluteUri || uri.Host.Equals(PlaceholderHost, StringComparison.OrdinalIgnoreCase))
		)
		{
			var httpContext =
				httpContextAccessor.HttpContext
				?? throw new InvalidOperationException(
					"No active HTTP request is available to resolve the Admin API origin. Configure AdminClientOptions.BaseUrl instead."
				);

			var origin = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}");
			var pathAndQuery = uri.IsAbsoluteUri ? uri.PathAndQuery : "/" + uri.OriginalString;
			request.RequestUri = new Uri(origin, pathAndQuery);
		}

		return base.SendAsync(request, cancellationToken);
	}
}
