// Example: Program.cs configuration for Admin Portal with Razor UI

using Purview.EventSourcing.Admin.Api;
using Purview.EventSourcing.Admin.Security;
using Purview.EventSourcing.Admin.Site;
using Purview.EventSourcing.Admin.SqlServer;
using Purview.EventSourcing.SqlServer.Events.EntityFramework;

var builder = WebApplicationBuilder.CreateBuilder(args);

// ============================================================================
// 1. Add Core Admin Portal Services
// ============================================================================

// Add options from configuration
builder.Services.Configure<AdminPortalOptions>(builder.Configuration.GetSection("AdminPortal"));

// Add Admin API (required)
builder.Services.AddPurviewEventSourcingAdminApi();

// ============================================================================
// 2. Add Security and Authorization
// ============================================================================

// Add admin authorization policies
builder
	.Services.AddAuthorization()
	.AddPurviewEventSourcingAdminSecurity()
	.AddSingleton<IAdminPermissionProvider, AllowAllPermissionProvider>();

// ============================================================================
// 3. Add Storage Adapter (choose one)
// ============================================================================

// Option A: SQL Server
builder.Services.AddPurviewEventSourcingAdminSqlServer();
builder.Services.AddDbContext<SqlServerEventStoreDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("EventStore"));
});

// Option B: MongoDB
// builder.Services.AddPurviewEventSourcingAdminMongoDB("EventStore");
// builder.Services.AddSingleton(_ =>
// {
//     var client = new MongoClient(builder.Configuration.GetConnectionString("MongoDB"));
//     return client;
// });

// ============================================================================
// 4. Add Admin Razor Pages UI (Optional)
// ============================================================================

// Enable Razor Pages and Admin.Site pages
builder.Services.AddPurviewEventSourcingAdminSite(enableRazorRuntimeCompilation: builder.Environment.IsDevelopment());

// ============================================================================
// 5. Build and Configure Application
// ============================================================================

var app = builder.Build();

// Middleware setup
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map API endpoints
app.MapPurviewEventSourcingAdminApi();

// Map Razor Pages (Admin.Site UI)
app.MapPurviewEventSourcingAdminSite();

// Standard endpoints
app.MapGet("/", () => "Admin Portal running").WithName("Health").WithOpenApi();

app.Run();

// ============================================================================
// Simple Authorization Policy: Allow All (Development Only)
// ============================================================================

public class AllowAllPermissionProvider : IAdminPermissionProvider
{
	public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
		ClaimsPrincipal user,
		CancellationToken cancellationToken
	)
	{
		var permissions = new List<AdminPermission>
		{
			new(AdminPortalPolicies.SearchAggregates, true),
			new(AdminPortalPolicies.ViewEvents, true),
			new(AdminPortalPolicies.ProjectPointInTime, true),
			new(AdminPortalPolicies.ExportData, true),
		};

		return Task.FromResult<IReadOnlyList<AdminPermission>>(permissions);
	}
}
