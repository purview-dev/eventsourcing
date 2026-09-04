namespace Purview.EventSourcing;

/// <summary>
/// Creates logical event-store transactions for coordinating multi-aggregate saves.
/// </summary>
public interface IEventStoreTransactionFactory
{
	/// <summary>
	/// Creates a new transaction.
	/// </summary>
	/// <param name="correlationId">
	/// Optional correlation ID shared by all enlisted aggregate saves.
	/// When <see langword="null"/>, the ambient correlation ID provider is consulted before generating a new correlation ID.
	/// </param>
	IEventStoreTransaction Create(string? correlationId = null);
}

/// <summary>
/// Default <see cref="IEventStoreTransactionFactory"/> implementation that consults the ambient correlation ID
/// provider when no explicit correlation ID is supplied.
/// </summary>
public sealed class EventStoreTransactionFactory(IEventStoreCorrelationIdProvider correlationIdProvider)
	: IEventStoreTransactionFactory
{
	/// <summary>
	/// Creates a new transaction.
	/// </summary>
	/// <param name="correlationId">
	/// Optional correlation ID shared by all enlisted aggregate saves.
	/// When <see langword="null"/>, the ambient correlation ID provider is consulted before generating a new correlation ID.
	/// </param>
	/// <returns>A new <see cref="EventStoreTransaction"/>.</returns>
	public IEventStoreTransaction Create(string? correlationId = null) =>
		new EventStoreTransaction(correlationId ?? correlationIdProvider.GetCorrelationId());
}
