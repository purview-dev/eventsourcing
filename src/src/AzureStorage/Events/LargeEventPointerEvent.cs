using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.AzureStorage.Events;

/// <summary>
/// An event that points to a large event payload stored in blob storage.
/// </summary>
/// <remarks>
/// When a serialized event exceeds the maximum table entity size, it is written to blob storage and a
/// <see cref="LargeEventPointerEvent"/> is persisted in its place so the payload can be located on replay.
/// </remarks>
public sealed class LargeEventPointerEvent : EventBase
{
	/// <summary>
	/// Gets or sets the name of the event type stored in the blob.
	/// </summary>
	public string SerializedEventType { get; set; } = default!;

	///<inheritdoc />
	protected override void BuildEventHash(ref HashCode hash) => hash.Add(SerializedEventType);
}
