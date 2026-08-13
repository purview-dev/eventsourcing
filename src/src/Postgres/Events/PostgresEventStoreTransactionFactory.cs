namespace Purview.EventSourcing.Postgres.Events;

/// <summary>
/// Creates PostgreSQL transaction coordinators that support enlisting additional SQL work
/// alongside event-store aggregate saves.
/// </summary>
public interface IPostgresEventStoreTransactionFactory
{
	/// <summary>
	/// Creates a new PostgreSQL transaction.
	/// </summary>
	/// <param name="correlationId">
	/// Optional correlation ID shared by all enlisted aggregate saves.
	/// When <see langword="null"/>, the ambient correlation ID provider is consulted before generating a new correlation ID.
	/// </param>
	IPostgresEventStoreTransaction CreatePostgresTransaction(string? correlationId = null);
}

public sealed class PostgresEventStoreTransactionFactory(
	IEventStoreCorrelationIdProvider correlationIdProvider
) : IEventStoreTransactionFactory, IPostgresEventStoreTransactionFactory
{
	public IEventStoreTransaction Create(string? correlationId = null) =>
		CreatePostgresTransaction(correlationId);

	public IPostgresEventStoreTransaction CreatePostgresTransaction(string? correlationId = null) =>
		new PostgresEventStoreTransaction(
			correlationId ?? correlationIdProvider.GetCorrelationId()
		);
}
