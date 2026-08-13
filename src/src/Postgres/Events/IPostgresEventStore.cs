using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Internal;

namespace Purview.EventSourcing.Postgres.Events;

public interface IPostgresEventStore<T>
	: INonQueryableEventStore<T>,
		IAggregateEventHistoryStoreCore<T>
	where T : class, IAggregate, new() { }
