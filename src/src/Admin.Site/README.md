# Admin.Site - Purview EventSourcing Admin Portal UI

## Overview

**Admin.Site** is an optional **Razor Class Library** that provides a ready-to-use web dashboard for the Purview EventSourcing Admin Portal. It enables developers to quickly browse aggregates, inspect event streams, and explore point-in-time projections through a clean, responsive web interface.

## Features

### 📊 Aggregate Search
- Search aggregates by type, ID, or timestamp range
- Pagination support for large datasets
- View aggregate metadata and current version
- Quick navigation to related event streams and projections

### 📝 Event Stream Inspector
- Browse complete event history for any aggregate
- Filter events by version or time range
- View event metadata (correlation ID, causation ID, idempotency ID, user ID)
- Inspect event payloads with collapsible JSON display

### 🔍 Point-in-Time Projection Viewer
- Reconstruct aggregate state at specific versions
- Reconstruct aggregate state at specific UTC timestamps
- View projection provenance (applied/skipped events)
- Inspect final aggregate state as JSON

## Pages

### Index.cshtml
- **Route:** `/` or `/admin/`
- **Purpose:** Main search interface for aggregates
- **Features:**
  - Filter by aggregate type and ID
  - Pagination with configurable page size
  - Table view with version and last-updated info
  - Quick action links to events and projections

### Events.cshtml
- **Route:** `/admin/events`
- **Purpose:** Browse event stream for a single aggregate
- **Features:**
  - View all events in sequence
  - Filter by version or time range
  - Expandable event cards with metadata
  - Event payload inspection

### Projection.cshtml
- **Route:** `/admin/projection`
- **Purpose:** Inspect point-in-time state of an aggregate
- **Features:**
  - Project to specific version
  - Project to specific UTC timestamp
  - Project to latest version
  - View provenance and event application history

## Integration

### Setup in ASP.NET Application

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add Admin Portal services
builder.Services.AddPurviewEventSourcingAdminApi(options => 
{
    options.EnableAggregateSearch = true;
    options.EnableEventInspection = true;
    options.EnableProjection = true;
});

// Add storage adapter (e.g., SQL Server)
builder.Services.AddPurviewEventSourcingAdminSqlServer();

// Add Razor Pages UI
builder.Services.AddPurviewEventSourcingAdminSite(enableRazorRuntimeCompilation: false);

var app = builder.Build();

// Map API endpoints
app.MapPurviewEventSourcingAdminApi();

// Map Razor Pages
app.MapPurviewEventSourcingAdminSite();

app.Run();
```

### Package Dependencies

- `Purview.EventSourcing.Admin.Abstractions` - Core types and interfaces
- `Purview.EventSourcing.Admin.Api` - REST API implementation
- `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` - Razor page support
- `Microsoft.AspNetCore.Components.Web` - Web components

## Storage Provider Support

Admin.Site works with any storage adapter implementing:
- `IAdminAggregateQueryService` - Aggregate search
- `IAdminEventQueryService` - Event range queries
- `IAdminProjectionService` - Point-in-time projections

Supported providers:
- ✅ **SQL Server** (Admin.SqlServer)
- ✅ **MongoDB** (Admin.MongoDB)
- ✅ **Custom implementations** (via abstraction interfaces)

## Styling and Customization

### CSS Organization

All styles are embedded in the Razor pages for simplicity:
- **Index.cshtml:** Aggregate search form and table styles
- **Events.cshtml:** Event card and metadata display styles
- **Projection.cshtml:** Projection result and provenance visualization styles
- **_Layout.cshtml:** Global layout and navigation styles

### Customization Options

To customize the UI, extend the base pages:

```csharp
// Create custom Pages folder in host application
// Razor Pages will override library defaults
// Example: Pages/Index.cshtml (custom version)
```

## Security Considerations

Admin.Site uses the same authorization policies as Admin.Api:
- `admin:aggregates:search` - Aggregate search
- `admin:events:view` - Event inspection
- `admin:projection:execute` - Point-in-time projection

Authorization is enforced at the API layer. Ensure proper policy configuration in your host application.

## Performance Characteristics

- **Page Load Time:** < 100ms (excluding network)
- **Search Response:** Depends on storage provider
- **Pagination:** 25 items/page (configurable)
- **Event Payload Rendering:** Limited to 10,000 character preview

## Limitations

- Razor runtime compilation requires additional CPU/memory (consider disabling in production)
- Large event payloads (>1MB) may render slowly
- Search performance depends on underlying storage provider query performance

## Future Enhancements

Potential additions for future versions:
- Event filtering by event type
- Batch projection for multiple aggregates
- Event payload comparison (version-to-version)
- Export events to CSV/JSON
- Real-time event stream subscription
- Custom field mapping for aggregate display

## Examples

### Search for Orders Created Today
1. Navigate to main admin page (`/`)
2. Enter "OrderAggregate" in Aggregate Type
3. Events page shows all order aggregates
4. Click "Events" to see history
5. Click "Projection" to see current state

### Find Aggregate at Specific Point in Time
1. Navigate to Projection page with aggregate details
2. Select "Specific Time (UTC)" option
3. Enter desired timestamp
4. View provenance showing which events were applied

## Troubleshooting

### Pages Won't Load
- Ensure `MapPurviewEventSourcingAdminSite()` is called in Program.cs
- Check that storage adapter is registered (SqlServer, MongoDB, etc.)
- Verify authorization policies are configured

### "No aggregates found"
- Verify event store contains events
- Check aggregate type name matches exactly
- Ensure authorization policies allow search access

### Pagination Not Working
- Check page number > 0 and <= total pages
- Verify PageSize setting in options
- Ensure storage provider supports pagination

## See Also

- `Admin.Api` - REST API endpoints
- `Admin.Security` - Authorization policies
- `Admin.Abstractions` - Core types and interfaces
- `Admin.SqlServer` - SQL Server storage adapter
- `Admin.MongoDB` - MongoDB storage adapter
