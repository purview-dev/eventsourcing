using Purview.EventSourcing.Aggregates.Events;
using Purview.EventSourcing.Serialization;

namespace Purview.EventSourcing.AzureStorage;

partial class TableEventStore<T>
{
	static IEvent? DeserializeEvent(string eventContent, Type eventType) =>
		EventStoreSerializationHelpers.Deserialize(eventContent, eventType) as IEvent;

	static async Task<IEvent?> DeserializeEventAsync(
		Stream eventStream,
		Type eventType,
		CancellationToken cancellationToken
	)
	{
		var result = await EventStoreSerializationHelpers.DeserializeAsync(eventStream, eventType, cancellationToken);
		return result as IEvent;
	}

	internal static string SerializeSnapshot(T aggregate) =>
		EventStoreSerializationHelpers.Serialize(aggregate, aggregate.GetType());

	internal static string SerializeEvent(IEvent @event) =>
		EventStoreSerializationHelpers.Serialize(@event, @event.GetType());

	static T DeserializeSnapshot(string aggregateContent) =>
		EventStoreSerializationHelpers.Deserialize<T>(aggregateContent)!;
}
