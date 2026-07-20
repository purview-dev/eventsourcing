# Purview.EventSourcing.CosmosDb

`Purview.EventSourcing.CosmosDb` adds Azure Cosmos DB queryable snapshot persistence to Purview EventSourcing.

## Install

```bash
dotnet add package Purview.EventSourcing.CosmosDb
```

## Register the provider

```csharp
builder.Services.AddCosmosDbQueryableEventStore();
```

## What it provides

- Query, list, count, and snapshot-backed reads through `IQueryableEventStore`
- Azure Cosmos DB persistence for aggregate read models
- Configuration binding for the Cosmos DB snapshot store

## Documentation

- [Repository README](https://github.com/kjldev/purview-eventsourcing/blob/main/README.md)
- [Provider feature matrix](https://github.com/kjldev/purview-eventsourcing/blob/main/docs/wiki/Provider-Feature-Matrix.md)

Snapshot query translation capabilities differ by provider; consult the provider matrix before relying on deep nested predicates for complex value-object shapes.
