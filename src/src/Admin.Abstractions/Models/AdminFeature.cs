namespace Purview.EventSourcing.Admin.Abstractions.Models;

/// <summary>
/// Identifies the capabilities exposed by the admin portal.
/// </summary>
public enum AdminFeature
{
	/// <summary>
	/// Search for aggregates across the event store.
	/// </summary>
	SearchAggregates,

	/// <summary>
	/// View the details of a single aggregate.
	/// </summary>
	ViewAggregate,

	/// <summary>
	/// View the event history of an aggregate stream.
	/// </summary>
	ViewEvents,

	/// <summary>
	/// View the serialized payloads contained in an aggregate's event history.
	/// </summary>
	ViewEventPayloads,

	/// <summary>
	/// Project aggregate state at a specific point in time.
	/// </summary>
	ProjectPointInTime,

	/// <summary>
	/// Export events from an aggregate stream.
	/// </summary>
	ExportEvents,
}
