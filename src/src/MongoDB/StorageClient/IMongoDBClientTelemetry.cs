using Purview.Telemetry;

namespace Purview.EventSourcing.MongoDB.StorageClient;

/// <summary>
/// Telemetry contract for the <see cref="MongoDBClient"/> storage client.
/// </summary>
/// <remarks>
/// Source-generated logging instrumentation used to trace low-level MongoDB client operations such as batch
/// writes, initialisation and delete operations.
/// </remarks>
[Logger]
public interface IMongoDBClientTelemetry
{
	/// <summary>
	/// Logs that a delete operation resulted in no documents being deleted.
	/// </summary>
	/// <param name="id">The identifier of the document targeted by the delete.</param>
	[Warning]
	void DeleteResultedInNoOp(string id);

	/// <summary>
	/// Logs that writing a batch of operations to MongoDB failed.
	/// </summary>
	/// <param name="exception">The exception that caused the failure.</param>
	[Error]
	void FailedToWriteBatch(Exception exception);

	/// <summary>
	/// Logs that the MongoDB client was initialised.
	/// </summary>
	void Initialized();

	/// <summary>
	/// Logs that the MongoDB event-store serializers were initialised.
	/// </summary>
	void EventsInitialized();
}
