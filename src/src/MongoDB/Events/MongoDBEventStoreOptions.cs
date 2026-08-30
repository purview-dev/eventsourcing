using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Purview.EventSourcing.MongoDB.Events;

/// <summary>
/// Configuration options for the MongoDB-backed <see cref="MongoDBEventStore{T}"/>.
/// </summary>
/// <remarks>
/// Bound from the <c>EventStore:MongoDB</c> configuration section by the default dependency-injection
/// registrations, and validated on start-up.
/// </remarks>
public sealed class MongoDBEventStoreOptions
{
	/// <summary>
	/// The configuration section the options are bound from.
	/// </summary>
	public const string MongoDBEventStore = "EventStore:MongoDB";

	const bool DefaultRemoveDeletedFromCache = true;
	const int DefaultEventSuffixLength = 30;

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
	/// The name of the MongoDB database that holds the store's collections.
	/// </summary>
	[Required]
	[RegularExpression(@"^[\w\-.]+$")]
	public string Database { get; set; } = default!;

	/// <summary>
	/// The name of the collection that stores aggregate events, stream version records and idempotency markers.
	/// </summary>
	/// <remarks>
	/// When null, a default collection name derived from the aggregate type is used.
	/// </remarks>
	[RegularExpression(@"^[\w\-.]+$")]
	public string? EventCollection { get; set; }

	/// <summary>
	/// The name of the collection that stores aggregate snapshots.
	/// </summary>
	/// <remarks>
	/// When null, a default collection name derived from the aggregate type is used.
	/// </remarks>
	[RegularExpression(@"^[\w\-.]+$")]
	public string? SnapshotCollection { get; set; }

	/// <summary>
	/// The optional MongoDB replica-set name.
	/// </summary>
	public string? ReplicaName { get; set; }

	/// <summary>
	/// The operation timeout in seconds.
	/// </summary>
	[Range(1, 120000)]
	public int? TimeoutInSeconds { get; set; } = 60;

	/// <summary>
	/// The maximum number of events to save in a single operation.
	/// </summary>
	[Range(1, 10_000)]
	public int MaxEventCountOnSave { get; set; } = 1000;

	/// <summary>
	/// <para>Indicates if a deleted aggregate is removed from cache. Defaults to true.</para>
	/// <para>
	/// If true, when an aggregate is deleted, it is removed from the cache.
	/// Or in the case of a get, it is not placed in cache for future calls.
	/// </para>
	/// <para>If false, a deleted aggregate can be placed into cache.</para>
	/// </summary>
	[DefaultValue(DefaultRemoveDeletedFromCache)]
	public bool RemoveDeletedFromCache { get; set; } = DefaultRemoveDeletedFromCache;

	/// <summary>
	/// The length of the suffix when creating event records.
	/// </summary>
	/// <remarks>Changing this where data already exists will result in incomplete aggregates.</remarks>
	[Required]
	[Range(10, 100)]
	[DefaultValue(DefaultEventSuffixLength)]
	public int EventSuffixLength { get; set; } = DefaultEventSuffixLength;

	/// <summary>
	/// Gets/ sets a value indicating how the <see cref="IEventStore{T}"/>
	/// uses the <see cref="IDistributedCache"/> during it's operations. Defaults to <see cref="SnapshotCachingOptions.GetAndStore"/>.
	/// </summary>
	[DefaultValue(SnapshotCachingOptions.GetAndStore)]
	public SnapshotCachingOptions CacheMode { get; set; } = SnapshotCachingOptions.GetAndStore;

	/// <summary>
	/// The default sliding expiration applied to cached aggregate snapshots.
	/// </summary>
	public TimeSpan DefaultCacheSlidingDuration { get; set; } = TimeSpan.FromMinutes(60);

	/// <summary>
	/// <para>
	/// Gets/ sets a value indicating if a valid identifier from a <see cref="ClaimsPrincipal"/> is required when
	/// saving aggregates.
	/// </para>
	/// <para>
	/// Sets the <see cref="EventStoreOperationContext.RequiresValidPrincipalIdentifier"/> to this value
	/// on the <see cref="EventStoreOperationContext.Default"/> property.
	/// </para>
	/// </summary>
	/// <remarks>If true and <see cref="IPrincipalService.Identifier()"/> returns null or empty string, an exception is thrown.</remarks>
	[DefaultValue(true)]
	public bool RequiresValidPrincipalIdentifier { get; set; } = true;
}
