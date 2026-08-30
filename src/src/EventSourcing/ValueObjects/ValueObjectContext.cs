namespace Purview.EventSourcing.ValueObjects;

/// <summary>
/// Provides the contextual information available when creating a contextual value object.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type providing the context.</typeparam>
/// <param name="Aggregate">The aggregate instance the value object is being created for.</param>
/// <param name="MemberName">The name of the aggregate member the value object is being assigned to.</param>
/// <param name="EventName">The optional name of the event that triggered the creation.</param>
public readonly record struct ValueObjectContext<TAggregate>(
	TAggregate Aggregate,
	string MemberName,
	string? EventName = null
);
