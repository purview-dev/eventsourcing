using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.MongoDB.Events;

/// <summary>
/// Builds the MongoDB database and collection names used by <see cref="MongoDBEventStore{T}"/> for
/// aggregate event and snapshot storage.
/// </summary>
/// <remarks>
/// Implementations are optional. When none is registered, the store falls back to the names configured on
/// <see cref="MongoDBEventStoreOptions"/>. Returning <see langword="null"/> from a method falls back to the
/// configured or default name for that element.
/// </remarks>
/// <seealso cref="MongoDBEventStoreOptions"/>
public interface IMongoDBEventStoreStorageNameBuilder
{
	/// <summary>
	/// Generates the collection name used to store aggregate events, stream versions and idempotency markers in.
	/// </summary>
	/// <typeparam name="T">The <see cref="IAggregate"/> type to generate the name for.</typeparam>
	/// <returns>The events collection name, or <see langword="null"/> to use the configured or default name.</returns>
	string? GetEventsCollectionName<T>();

	/// <summary>
	/// Generates the collection name used to store aggregate snapshots in.
	/// </summary>
	/// <typeparam name="T">The <see cref="IAggregate"/> type to generate the name for.</typeparam>
	/// <returns>The snapshot collection name, or <see langword="null"/> to use the configured or default name.</returns>
	string? GetSnapshotCollectionName<T>();

	/// <summary>
	/// Generates the name of the MongoDB database used to store the events and other data associated with
	/// the <see cref="IMongoDBEventStore{T}"/>.
	/// </summary>
	/// <typeparam name="T">The <see cref="IAggregate"/> type to generate the name for.</typeparam>
	/// <returns>A MongoDB database name, or <see langword="null"/> to use the configured database.</returns>
	string? GetDatabaseName<T>();
}
