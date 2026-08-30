using Purview.Telemetry;

namespace Purview.EventSourcing.MongoDB.Snapshots;

/// <summary>
/// Telemetry contract for the <see cref="MongoDBSnapshotEventStore{T}"/> implementation.
/// </summary>
/// <remarks>
/// Source-generated metrics instrumentation used to observe snapshot creation.
/// </remarks>
[Meter]
public interface IMongoDBSnapshotEventStoreTelemetry
{
	/// <summary>
	/// Records that a snapshot was created.
	/// </summary>
	/// <param name="aggregateType">The short name of the aggregate type the snapshot belongs to.</param>
	[Counter(AutoIncrement = true)]
	void SnapshotCreated(string aggregateType);
}
