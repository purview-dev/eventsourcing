using System.Diagnostics;
using Purview.Telemetry;

namespace Purview.EventSourcing.Postgres.Snapshots;

/// <summary>
/// Telemetry contract (activities, counters, and log messages) for the PostgreSQL snapshot event store.
/// </summary>
[ActivitySource]
[Logger]
[Meter]
public interface IPostgresSnapshotEventStoreTelemetry
{
	// Activities (distributed tracing)

	/// <summary>
	/// Starts an activity for a snapshot save operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being snapshotted.</param>
	/// <param name="aggregateType">The type of the aggregate being snapshotted.</param>
	/// <returns>The started activity, or <see langword="null"/> when the activity source is disabled.</returns>
	[Activity]
	Activity? SnapshotSave(string aggregateId, [Baggage] string aggregateType);

	/// <summary>
	/// Starts an activity for a snapshot query operation.
	/// </summary>
	/// <param name="aggregateType">The type of the aggregate being queried.</param>
	/// <returns>The started activity, or <see langword="null"/> when the activity source is disabled.</returns>
	[Activity]
	Activity? SnapshotQuery([Baggage] string aggregateType);

	/// <summary>
	/// Starts an activity for a snapshot delete operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being deleted.</param>
	/// <param name="aggregateType">The type of the aggregate being deleted.</param>
	/// <returns>The started activity, or <see langword="null"/> when the activity source is disabled.</returns>
	[Activity]
	Activity? SnapshotDelete(string aggregateId, [Baggage] string aggregateType);

	/// <summary>
	/// Records the completion of a snapshot query.
	/// </summary>
	/// <param name="activity">The query activity to attach the event to.</param>
	/// <param name="aggregateType">The type of the aggregate that was queried.</param>
	/// <param name="resultCount">The number of results returned.</param>
	/// <param name="elapsedMilliseconds">The duration of the query in milliseconds.</param>
	[Debug]
	[AutoCounter]
	[Event]
	void QueryCompleted(
		Activity? activity,
		[ExcludeTargets(Targets.Activities)] string aggregateType,
		int resultCount,
		[ExcludeTargets(Targets.Activities)] long elapsedMilliseconds
	);

	// Logging

	/// <summary>
	/// Logs the start of a snapshot save operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being snapshotted.</param>
	/// <param name="aggregateType">The type of the aggregate being snapshotted.</param>
	[Debug]
	void SnapshotSaveStart(string aggregateId, string aggregateType);

	/// <summary>
	/// Logs the successful completion of a snapshot save operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate that was snapshotted.</param>
	/// <param name="aggregateType">The type of the aggregate that was snapshotted.</param>
	[Debug]
	[AutoCounter]
	void SnapshotSaveComplete(string aggregateId, string aggregateType);

	/// <summary>
	/// Logs a failed snapshot save operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate that failed to be snapshotted.</param>
	/// <param name="aggregateType">The type of the aggregate that failed to be snapshotted.</param>
	/// <param name="exception">The exception that caused the failure.</param>
	[Error]
	void SnapshotSaveFailed(string aggregateId, string aggregateType, Exception exception);

	/// <summary>
	/// Logs the start of a snapshot delete operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being deleted.</param>
	/// <param name="aggregateType">The type of the aggregate being deleted.</param>
	[Debug]
	void SnapshotDeleteStart(string aggregateId, string aggregateType);

	/// <summary>
	/// Logs the successful completion of a snapshot delete operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate that was deleted.</param>
	/// <param name="aggregateType">The type of the aggregate that was deleted.</param>
	[Debug]
	[AutoCounter]
	void SnapshotDeleteComplete(string aggregateId, string aggregateType);

	/// <summary>
	/// Logs a failed snapshot delete operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate that failed to be deleted.</param>
	/// <param name="aggregateType">The type of the aggregate that failed to be deleted.</param>
	/// <param name="exception">The exception that caused the failure.</param>
	[Error]
	void SnapshotDeleteFailed(string aggregateId, string aggregateType, Exception exception);

	/// <summary>
	/// Logs the start of a snapshot query operation.
	/// </summary>
	/// <param name="aggregateType">The type of the aggregate being queried.</param>
	/// <param name="maxRecords">The maximum number of records requested.</param>
	[Debug]
	void SnapshotQueryStart(string aggregateType, int maxRecords);

	/// <summary>
	/// Logs the successful completion of a snapshot query operation.
	/// </summary>
	/// <param name="aggregateType">The type of the aggregate that was queried.</param>
	/// <param name="resultCount">The number of results returned.</param>
	/// <param name="elapsedMilliseconds">The duration of the query in milliseconds.</param>
	[Debug]
	[AutoCounter]
	void SnapshotQueryComplete(string aggregateType, int resultCount, long elapsedMilliseconds);

	/// <summary>
	/// Logs a failed snapshot query operation.
	/// </summary>
	/// <param name="aggregateType">The type of the aggregate that failed to be queried.</param>
	/// <param name="exception">The exception that caused the failure.</param>
	[Error]
	void SnapshotQueryFailed(string aggregateType, Exception exception);
}
