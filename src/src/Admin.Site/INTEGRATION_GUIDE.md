# Admin.Site Integration Examples

## Basic Setup in Program.cs

The following example shows how to configure Admin.Site in your ASP.NET application:

```csharp
using Purview.EventSourcing.Admin.Site;
using Purview.EventSourcing.Admin.Api;
using Purview.EventSourcing.Admin.Security;
using Purview.EventSourcing.Admin.SqlServer;

var builder = WebApplicationBuilder.CreateBuilder(args);

// 1. Add Core Admin Portal Services
builder.Services.Configure<AdminPortalOptions>(
    builder.Configuration.GetSection("AdminPortal")
);
builder.Services.AddPurviewEventSourcingAdminApi();

// 2. Add Security and Authorization
builder.Services
    .AddAuthorization()
    .AddPurviewEventSourcingAdminSecurity()
    .AddSingleton<IAdminPermissionProvider, AllowAllPermissionProvider>();

// 3. Add Storage Adapter (SQL Server example)
builder.Services.AddPurviewEventSourcingAdminSqlServer();
builder.Services.AddDbContext<SqlServerEventStoreDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EventStore"));
});

// 4. Add Admin Razor Pages UI
builder.Services.AddPurviewEventSourcingAdminSite(
    enableRazorRuntimeCompilation: builder.Environment.IsDevelopment()
);

var app = builder.Build();

// Setup middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map API endpoints
app.MapPurviewEventSourcingAdminApi();

// Map Razor Pages UI
app.MapPurviewEventSourcingAdminSite();

app.Run();
```

## With MongoDB Adapter

To use Admin.Site with MongoDB:

```csharp
// Instead of SQL Server setup:
builder.Services.AddPurviewEventSourcingAdminMongoDB("EventStore");
builder.Services.AddSingleton(_ =>
{
    var client = new MongoClient(
        builder.Configuration.GetConnectionString("MongoDB")
    );
    return client;
});
```

## Configuration (appsettings.json)

```json
{
  "AdminPortal": {
    "Enabled": true,
    "ApiPathPrefix": "/api/admin",
    "EnableAggregateSearch": true,
    "EnableEventInspection": true,
    "EnableProjection": true,
    "Paging": {
      "DefaultPageSize": 25,
      "MaxPageSize": 100
    },
    "Projection": {
      "MaxEventsToReplay": 10000,
      "TimeoutSeconds": 30
    }
  },
  "ConnectionStrings": {
    "EventStore": "Server=.;Database=EventStore;Trusted_Connection=true;"
  }
}
```

## Simple Permission Provider (Development)

For development, you can use a simple allow-all permission provider:

```csharp
using System.Security.Claims;
using Purview.EventSourcing.Admin.Abstractions;
using Purview.EventSourcing.Admin.Security;

public class AllowAllPermissionProvider : IAdminPermissionProvider
{
    public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
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
```

## Custom Permission Provider (Production)

For production, implement role-based access:

```csharp
public class RoleBasedPermissionProvider : IAdminPermissionProvider
{
    public Task<IReadOnlyList<AdminPermission>> GetPermissionsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var permissions = new List<AdminPermission>();

        // Admin role - full access
        if (user.IsInRole("EventSourcingAdmin"))
        {
            permissions.Add(new(AdminPortalPolicies.SearchAggregates, true));
            permissions.Add(new(AdminPortalPolicies.ViewEvents, true));
            permissions.Add(new(AdminPortalPolicies.ProjectPointInTime, true));
            permissions.Add(new(AdminPortalPolicies.ExportData, true));
        }
        
        // Support role - read-only
        else if (user.IsInRole("Support"))
        {
            permissions.Add(new(AdminPortalPolicies.SearchAggregates, true));
            permissions.Add(new(AdminPortalPolicies.ViewEvents, true));
            permissions.Add(new(AdminPortalPolicies.ProjectPointInTime, true));
        }

        return Task.FromResult<IReadOnlyList<AdminPermission>>(permissions);
    }
}
```

## UI Access

After setup, access the Admin Portal at:
- **Dashboard:** `https://yourapp/` (redirects from root if configured)
- **Search:** `https://yourapp/admin/`
- **Events:** `https://yourapp/admin/events?aggregateType=OrderAggregate&aggregateId=123`
- **Projection:** `https://yourapp/admin/projection?aggregateType=OrderAggregate&aggregateId=123`

## Docker Deployment Example

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["YourApp.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

## Troubleshooting

### Pages return 404
- Ensure `app.MapPurviewEventSourcingAdminSite()` is called
- Check route configuration in options
- Verify Admin.Site package is referenced

### Authorization denied
- Verify permission provider is registered
- Check user has required roles
- Review authorization policies

### No data appears
- Ensure storage adapter is configured correctly
- Verify connection string
- Check that event store contains events

## See Also

- [Admin.Api Documentation](../Admin.Api/README.md)
- [Admin.Security Documentation](../Admin.Security/README.md)
- [Admin.SqlServer Documentation](../Admin.SqlServer/README.md)
- [Admin.MongoDB Documentation](../Admin.MongoDB/README.md)
