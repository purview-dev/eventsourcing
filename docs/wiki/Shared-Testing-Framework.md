# Shared Testing Framework

This page describes the shared provider-agnostic test framework used by the storage-provider
integration suites.

## Overview

Each storage provider (`AzureStorage`, `CosmosDb`, `MongoDB`, `Postgres`, `SqlServer`) has its own
integration test project. Instead of duplicating the same behavioural tests for every provider, the
repository defines two shared contract suites that run against every provider that advertises the
relevant capability:

- **Event-store contract suite** — runs against providers with an event store: Azure Storage,
  MongoDB, Postgres, SQL Server. (Cosmos DB has no event stream.)
- **Snapshot-store contract suite** — runs against providers with a query snapshot store: Cosmos DB,
  MongoDB, Postgres, SQL Server. (Azure Storage has no query snapshot store.)

Provider-specific behaviour (batch limits, index creation, JSON operators, query-translation
boundaries, telemetry, storage layout) lives in per-provider guard tests in each integration project.

## Layout

| Path | Purpose |
| --- | --- |
| `src/tests/SharedTestingFramework/Contracts/` | The shared contract suites. These are compiled into the `SharedTestingFramework` assembly and consumed by the provider integration test projects through the existing project reference. |
| `src/tests/SharedTestingFramework/Fixtures/` | Provider Testcontainers fixtures. |
| `src/tests/<Provider>.IntegrationTests/Events/` | The per-provider event-store wiring + guard tests. |
| `src/tests/<Provider>.IntegrationTests/Snapshots/` | The per-provider snapshot-store wiring + guard tests. |
| `src/tests/<Provider>.IntegrationTests/Guards/` | Provider-specific event-store guard tests. |

## How the shared suites are wired

TUnit uses compile-time discovery, so the `[Test]` methods must be discoverable from the test
assembly. The shared suites achieve this with three TUnit features:

1. `[GenerateGenericTest(typeof(PersistenceAggregate))]` on a generic test class makes TUnit
   generate a concrete test class for the supplied aggregate type.
2. `[ClassDataSource<TFixture>(Shared = SharedType.PerTestSession)]` injects the provider fixture
   (one container per test session).
3. `[InheritsTests]` picks up the `[Test]` methods declared on the shared generic base class
   (`EventStoreContractTestsBase<TAggregate>` / `SnapshotStoreContractTestsBase<TAggregate>`), which
   lives in the referenced `SharedTestingFramework` assembly.

`SharedTestingFramework` carries the base `[Test]` methods but its own tests are skipped by the
SDK's shared-testing `[Skip]` assembly attribute; the methods are only exercised through the
concrete derived classes in the provider test projects.

Each provider test project therefore adds a small derived class such as:

```csharp
[GenerateGenericTest(typeof(PersistenceAggregate))]
[ClassDataSource<SqlServerEventStoreFixture>(Shared = SharedType.PerTestSession)]
[InheritsTests]
public sealed class EventStoreContractTests<TAggregate>(SqlServerEventStoreFixture fixture)
    : EventStoreContractTestsBase<TAggregate>
    where TAggregate : class, IAggregateTest, new()
{
    protected override IEventStoreCore<TAggregate> CreateEventStore() => fixture.CreateEventStore<TAggregate>();

    protected override IEventStoreCore<TAggregate> CreateEventStore(IAggregateChangeFeedNotifier<TAggregate>? notifier) =>
        fixture.CreateEventStore(aggregateChangeNotifier: notifier);

    protected override Task MarkEventTypesAsUnknownAsync(...) => /* provider-specific event rewrite */;
}
```

The shared base classes only exercise the public contracts (`IEventStoreCore<T>` and
`IQueryableEventStoreCore<T>`), observable state (save results, rehydrated aggregates, change-feed
notifications, event ranges, query results) and shared aggregates (`PersistenceAggregate`,
`ComplexTestType`). Provider internals are deliberately out of scope for the shared suites.

## Where each suite runs

| Suite | AzureStorage | CosmosDb | MongoDB | Postgres | SqlServer |
| --- | --- | --- | --- | --- | --- |
| Event-store contract suite | ✓ | — | ✓ | ✓ | ✓ |
| Snapshot-store contract suite | — | ✓ | ✓ | ✓ | ✓ |
| Provider guard / feature tests | ✓ | ✓ | ✓ | ✓ | ✓ |

## Adding a new provider

1. Create the integration test project (see `project-placement-defaults`), referencing
   `SharedTestingFramework` and `Samples`.
2. Add an `EventStoreContractTests` (and/or `SnapshotStoreContractTests`) derived class wired to the
   provider fixture.
3. Implement the provider-specific seams (`MarkEventTypesAsUnknownAsync` for the unknown-event test,
   `SnapshotAsync` for the snapshot suite).
4. Add guard tests for capabilities that are not part of the shared contract.

## Adding a shared test

1. Add the `[Test]` method to the relevant shared base class under `Contracts/`.
2. Add any data sources to the matching `*ContractTestData` static class.
3. The test runs automatically for every provider that references `SharedTestingFramework` and
   derives from the relevant base class.

## Environment notes

- Integration suites require Docker and the provider images (Testcontainers).
- The Azure/Cosmos/Mongo snapshot fixtures rely on Azurite; the SQL fixtures on a SQL Server image.
- The CI pipeline runs only the unit-test tree filter; integration suites are exercised locally or
  via an opt-in run.