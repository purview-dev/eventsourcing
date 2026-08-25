using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Fixtures.AzureStorage;

namespace Purview.EventSourcing.AzureStorage;

[ClassDataSource<TableEventStoreFixture>(Shared = SharedType.PerTestSession)]
public partial class GenericTableEventStoreTests<TAggregate>(TableEventStoreFixture fixture) : ITableEventStoreTests
	where TAggregate : class, IAggregateTest, new()
{
	// Here to stop IDE0055
}
