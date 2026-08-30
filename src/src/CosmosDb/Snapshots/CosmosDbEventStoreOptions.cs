using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.CosmosDb.Snapshots;

/// <summary>
/// Options for the Azure Cosmos DB snapshot event store.
/// </summary>
/// <remarks>
/// Bound from the <c>"EventStore:CosmosDbSnapshot"</c> configuration section. The
/// <see cref="CosmosDbOptions.PartitionKeyPath"/> defaults to <c>"/AggregateType"</c> so that
/// snapshot documents are partitioned by aggregate type.
/// </remarks>
public sealed class CosmosDbEventStoreOptions : CosmosDbOptions
{
	/// <summary>
	/// The configuration section key used to bind <see cref="CosmosDbEventStoreOptions"/>.
	/// </summary>
	public const string CosmosDbEventStore = "EventStore:CosmosDbSnapshot";

	/// <summary>
	/// Creates a new instance of <see cref="CosmosDbEventStoreOptions"/> with the default partition key path.
	/// </summary>
	public CosmosDbEventStoreOptions()
	{
		PartitionKeyPath = $"/{nameof(IAggregate.AggregateType)}";
	}
}
