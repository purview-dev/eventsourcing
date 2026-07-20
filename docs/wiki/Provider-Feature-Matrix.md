# Provider Feature Matrix

This page summarizes feature availability by package so provider selection is explicit and accurate.

| Capability | `Purview.EventSourcing` (core) | `Purview.EventSourcing.SqlServer` | `Purview.EventSourcing.AzureStorage` | `Purview.EventSourcing.MongoDB` | `Purview.EventSourcing.CosmosDb` |
| --- | --- | --- | --- | --- | --- |
| Aggregate/event abstractions (`AggregateBase`, `EventBase`) | Yes | Uses core | Uses core | Uses core | Uses core |
| Provider-agnostic event facade (`IEventStore`) | Yes | SQL event store | Azure Table event store | MongoDB event store | Optional registration via snapshot provider |
| Provider-agnostic query facade (`IQueryableEventStore`) | Yes (interface + null implementation) | SQL snapshot store | Not provided | MongoDB snapshot store | Cosmos snapshot store |
| Event-stream persistence | Not persistent by itself | Yes | Yes | Yes | No |
| Snapshot-backed query/list/count | Null provider only | Yes | No | Yes | Yes |
| Blob-backed snapshots / large payloads | No | No | Yes | No | No |
| SQL-specific transaction factory (`ISqlServerEventStoreTransactionFactory`) | No | Yes | No | No | No |
| DI registration helpers | `AddNullQueryableEventStore()` | `AddSqlServerEventStore()`, `AddSqlServerSnapshotQueryableEventStore()` | `AddAzureTableEventStore()` | `AddMongoDBEventStore()`, `AddMongoDBSnapshotQueryableEventStore()` | `AddCosmosDbQueryableEventStore()` |

## Selection guidance

- Choose **SQL Server** when you need event streams and queryable snapshots with SQL-native transaction coordination.
- Choose **Azure Storage** when you want Azure Table event persistence with Blob support for large payloads/snapshots.
- Choose **MongoDB** when you want both event and snapshot stores on MongoDB.
- Choose **Cosmos DB** when you only need a queryable snapshot store.

### SQL Server query translation notes

- SQL snapshot queries support predicates over JSON-mapped primitive members and supported value-object shapes.
- For provider-converted members (for example, a `[Scalar]` value object whose inner `Value` is a complex type), deep predicates on inner members are **not SQL-translatable** (for example: `a.ReportSummary.Value.ParserDetails.FailedLines > 0`).
- The same conceptual data **can** be queried deeply when exposed as a directly mapped complex property in the snapshot graph (for example: `a.ReportSummaryScalar.ParserDetails.FailedLines > 0`, where `ReportSummaryScalar` is a `ParserReportSummary`).
- `EventStoreList<T>` / `EventStoreSet<T>` members with `[ValueObject]` struct elements are persisted via JSON conversion for compatibility; treat nested element member filtering as non-translatable unless explicitly covered by tests.
- Nested dictionary/interface-collection members inside directly mapped complex snapshot graphs require explicit mapper support; when supported, prove the exact predicate path with provider integration tests.
- Recommended pattern: query by SQL-translatable fields first, or expose a directly mapped complex mirror property when deep SQL filtering is a real requirement.

## Related docs

- [Getting Started](Getting-Started.md)
- [SQL Server Guide](SQL-Server-Guide.md)
- Core package README: `src/src/EventSourcing/README.md`
- SQL Server package README: `src/src/EventSourcing.SqlServer/README.md`
- Azure Storage package README: `src/src/EventSourcing.AzureStorage/README.md`
- MongoDB package README: `src/src/EventSourcing.MongoDB/README.md`
- Cosmos DB package README: `src/src/EventSourcing.CosmosDb/README.md`
