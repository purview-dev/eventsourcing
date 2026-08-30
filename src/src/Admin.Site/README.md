# Purview.EventSourcing.Admin.Site

`Purview.EventSourcing.Admin.Site` is an optional Razor Class Library that provides a ready-to-use web UI for the Purview EventSourcing admin portal. It lets you browse aggregates, inspect event streams, and explore point-in-time projections through Razor Pages.

## Install

```bash
dotnet add package Purview.EventSourcing.Admin.Site
```

## Register and map the UI

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the admin services (API options, storage adapter, security).
builder.Services.AddPurviewEventSourcingAdminApi();
builder.Services.AddPurviewEventSourcingAdminSqlServer();
builder.Services.AddPurviewEventSourcingAdminSecurity();

// Register the Razor Pages UI.
builder.Services.AddPurviewEventSourcingAdminSite(enableRazorRuntimeCompilation: false);

var app = builder.Build();

// Map the admin API endpoints and the Razor Pages UI.
app.MapPurviewEventSourcingAdminAPI();
app.MapPurviewEventSourcingAdminSite(); // defaults to the /admin prefix

app.Run();
```

`MapPurviewEventSourcingAdminSite` accepts a `pathPrefix` (defaults to `/admin`).

## Pages

### Index (`/admin/`)

The aggregate search page.

- Filter by aggregate type and aggregate id
- Paginated table of matching aggregates with version and last-updated information
- Navigation links to the event-history and projection pages for each aggregate

### Events (`/admin/events`)

The event-stream inspector for a single aggregate.

- Lists the event history in version order
- Displays event metadata (aggregate version, timestamp, idempotency id, user id, causation id, correlation id)
- Shows each event payload as collapsible JSON

### Projection (`/admin/projection`)

The point-in-time projection viewer.

- Project aggregate state at a specific stream version
- Project aggregate state at a specific UTC timestamp
- Shows projection provenance (applied/skipped events) and the final aggregate state as JSON

## Requirements

The UI relies on the admin service contracts from `Purview.EventSourcing.Admin.Abstractions`:

- `IAdminAggregateQueryService` - aggregate search
- `IAdminEventQueryService` - event-range queries
- `IAdminProjectionService` - point-in-time projections

A storage adapter must be registered to provide these services. Supported adapters:

- `Purview.EventSourcing.Admin.SqlServer`
- `Purview.EventSourcing.Admin.MongoDB`
- `Purview.EventSourcing.Admin.Postgres`
- `Purview.EventSourcing.Admin.AzureStorage`
- Any custom implementation of the abstraction contracts

## Authorization

Authorization is enforced at the API layer using the policies defined in `Purview.EventSourcing.Admin.Security`. Ensure the policies are registered and your `IAdminPermissionProvider` grants the relevant `AdminFeature` permissions (search, aggregate view, event view, projection).

## Related packages

- [Admin abstractions](https://github.com/kjldev/purview-eventsourcing/blob/main/src/src/Admin.Abstractions/README.md): `Purview.EventSourcing.Admin.Abstractions`
- [Admin API](https://github.com/kjldev/purview-eventsourcing/blob/main/src/src/Admin.API/README.md): `Purview.EventSourcing.Admin.Api`
- [Admin security](https://github.com/kjldev/purview-eventsourcing/blob/main/src/src/Admin.Security/README.md): `Purview.EventSourcing.Admin.Security`