using System.Diagnostics;
using Purview.Telemetry;

namespace Purview.EventSourcing.SqlServer.Snapshots;

/// <summary>
/// Telemetry contract for the SQL Server snapshot event store, covering distributed tracing, metrics, and logging.
/// </summary>
/// <remarks>
/// Implemented by the source generator to emit activities, counters, and log statements for
/// <see cref="SqlServerSnapshotEventStore{T}"/> operations.
/// </remarks>
[ActivitySource]
[Logger]
[Meter]
public interface ISqlServerSnapshotEventStoreTelemetry
{
	// Activities (distributed tracing)

	/// <summary>
	/// Starts an activity representing a snapshot-save operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being snapshotted.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <returns>The activity, or <see langword="null"/> when tracing is not enabled.</returns>
	[Activity]
	Activity? SnapshotSave(string aggregateId, [Baggage] string aggregateType);

	/// <summary>
	/// Starts an activity representing a snapshot-query operation.
	/// </summary>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <returns>The activity, or <see langword="null"/> when tracing is not enabled.</returns>
	[Activity]
	Activity? SnapshotQuery([Baggage] string aggregateType);

	/// <summary>
	/// Starts an activity representing a snapshot-delete operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being removed.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <returns>The activity, or <see langword="null"/> when tracing is not enabled.</returns>
	[Activity]
	Activity? SnapshotDelete(string aggregateId, [Baggage] string aggregateType);

	/// <summary>
	/// Records the completion of a snapshot query.
	/// </summary>
	/// <param name="activity">The activity to attach the event to.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <param name="resultCount">The number of results returned.</param>
	/// <param name="elapsedMilliseconds">The elapsed time of the query in milliseconds.</param>
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
	/// Logs the start of a snapshot-save operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being snapshotted.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	[Debug]
	void SnapshotSaveStart(string aggregateId, string aggregateType);

	/// <summary>
	/// Logs the completion of a snapshot-save operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being snapshotted.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	[Debug]
	[AutoCounter]
	void SnapshotSaveComplete(string aggregateId, string aggregateType);

	/// <summary>
	/// Logs a failure during a snapshot-save operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being snapshotted.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <param name="exception">The exception that was thrown.</param>
	[Error]
	void SnapshotSaveFailed(string aggregateId, string aggregateType, Exception exception);

	/// <summary>
	/// Logs the start of a snapshot-delete operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being removed.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	[Debug]
	void SnapshotDeleteStart(string aggregateId, string aggregateType);

	/// <summary>
	/// Logs the completion of a snapshot-delete operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being removed.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	[Debug]
	[AutoCounter]
	void SnapshotDeleteComplete(string aggregateId, string aggregateType);

	/// <summary>
	/// Logs a failure during a snapshot-delete operation.
	/// </summary>
	/// <param name="aggregateId">The id of the aggregate being removed.</param>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <param name="exception">The exception that was thrown.</param>
	[Error]
	void SnapshotDeleteFailed(string aggregateId, string aggregateType, Exception exception);

	/// <summary>
	/// Logs the start of a snapshot query.
	/// </summary>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <param name="maxRecords">The maximum number of records requested.</param>
	[Debug]
	void SnapshotQueryStart(string aggregateType, int maxRecords);

	/// <summary>
	/// Logs the completion of a snapshot query.
	/// </summary>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <param name="resultCount">The number of results returned.</param>
	/// <param name="elapsedMilliseconds">The elapsed time of the query in milliseconds.</param>
	[Debug]
	[AutoCounter]
	void SnapshotQueryComplete(string aggregateType, int resultCount, long elapsedMilliseconds);

	/// <summary>
	/// Logs a failure during a snapshot query.
	/// </summary>
	/// <param name="aggregateType">The short name of the aggregate type.</param>
	/// <param name="exception">The exception that was thrown.</param>
	[Error]
	void SnapshotQueryFailed(string aggregateType, Exception exception);
}
