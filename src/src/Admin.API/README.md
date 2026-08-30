# Purview.EventSourcing.Admin.Api

`Purview.EventSourcing.Admin.Api` provides minimal-API endpoints for the Purview EventSourcing admin portal: aggregate search, event-history inspection, and point-in-time projection.

## Install

```bash
dotnet add package Purview.EventSourcing.Admin.Api
```

## Register and map the endpoints

```csharp
builder.Services.AddPurviewEventSourcingAdminApi(options =>
{
    options.Enabled = true;
    options.RoutePrefix = "/admin/api";
    options.Features.SearchAggregates = true;
    options.Features.ViewEvents = true;
    options.Features.ProjectPointInTime = true;
});

var app = builder.Build();

app.MapPurviewEventSourcingAdminAPI();
```

## Configuration

`AdminPortalOptions` binds from the `AdminPortal` configuration section and can also be configured inline. Key settings:

- `Enabled` - master switch; when `false`, no endpoints are mapped (default `true`)
- `RoutePrefix` - route group prefix (default `/admin/api`)
- `Features` - per-capability toggles for search, aggregate view, event history, point-in-time projection, and export
- `Paging` - `DefaultPageSize` (default `50`) and `MaxPageSize` (default `500`)
- `Projections` - `MaxVersionsPerQuery` (default `10000`) and `MaxTimeRangePerQuery` (default 365 days)

```json
{
  "AdminPortal": {
    "Enabled": true,
    "RoutePrefix": "/admin/api",
    "Features": {
      "SearchAggregates": true,
      "ViewEvents": true,
      "ProjectPointInTime": true
    }
  }
}
```

## Endpoints

All endpoints are grouped under `RoutePrefix` and require authorization:

- Search aggregates: `GET <prefix>/aggregates`
- Aggregate details: `GET <prefix>/aggregates/{aggregateType}/{aggregateId}`
- Event range: `GET <prefix>/aggregates/{aggregateType}/{aggregateId}/events`
- Project at version: `GET <prefix>/aggregates/{aggregateType}/{aggregateId}/projection/version/{version}`
- Project at time: `GET <prefix>/aggregates/{aggregateType}/{aggregateId}/projection/time/{timestamp}`

## Dependencies

You must also register:

- An admin storage adapter (for example `Purview.EventSourcing.Admin.SqlServer`) that provides the `IAdminAggregateQueryService`, `IAdminEventQueryService`, and `IAdminProjectionService` implementations.
- The authorization policies defined in `Purview.EventSourcing.Admin.Security`.

## Related packages

- [Admin abstractions](https://github.com/kjldev/purview-eventsourcing/blob/main/src/src/Admin.Abstractions/README.md): `Purview.EventSourcing.Admin.Abstractions`
- [Admin security](https://github.com/kjldev/purview-eventsourcing/blob/main/src/src/Admin.Security/README.md): `Purview.EventSourcing.Admin.Security`
- [Admin UI](https://github.com/kjldev/purview-eventsourcing/blob/main/src/src/Admin.Site/README.md): `Purview.EventSourcing.Admin.Site`
- Storage adapters: `Purview.EventSourcing.Admin.SqlServer`, `Purview.EventSourcing.Admin.MongoDB`, `Purview.EventSourcing.Admin.Postgres`, `Purview.EventSourcing.Admin.AzureStorage`