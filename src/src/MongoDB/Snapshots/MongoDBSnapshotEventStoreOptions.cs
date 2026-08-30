using System.ComponentModel.DataAnnotations;

namespace Purview.EventSourcing.MongoDB.Snapshots;

/// <summary>
/// Configuration options for the MongoDB-backed <see cref="MongoDBSnapshotEventStore{T}"/>.
/// </summary>
/// <remarks>
/// Bound from the <c>EventStore:MongoDBSnapshot</c> configuration section by the default dependency-injection
/// registrations, and validated on start-up.
/// </remarks>
public sealed class MongoDBSnapshotEventStoreOptions
{
	/// <summary>
	/// The configuration section the options are bound from.
	/// </summary>
	public const string MongoDBEventStore = "EventStore:MongoDBSnapshot";

	/// <summary>
	/// The MongoDB connection string used by the store.
	/// </summary>
	[Required]
	public string ConnectionString { get; set; } = default!;

	/// <summary>
	/// The optional application name reported to the MongoDB server.
	/// </summary>
	public string? ApplicationName { get; set; }

	/// <summary>
	/// The name of the MongoDB database that holds the snapshot collection.
	/// </summary>
	[Required]
	[RegularExpression(@"^[\w\-.]+$")]
	public string Database { get; set; } = default!;

	/// <summary>
	/// The name of the collection that stores aggregate snapshot documents.
	/// </summary>
	/// <remarks>
	/// When null, a default collection name derived from the aggregate type is used.
	/// </remarks>
	[RegularExpression(@"^[\w\-.]+$")]
	public string? Collection { get; set; }
}
