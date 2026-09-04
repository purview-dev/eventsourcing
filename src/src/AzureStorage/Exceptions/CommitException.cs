namespace Purview.EventSourcing.AzureStorage.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when a batch of events could not be committed to Azure Table Storage.
/// </summary>
/// <param name="errorCode">The HTTP status code returned by the storage operation.</param>
/// <param name="aggregateId">The id of the aggregate.</param>
/// <param name="idempotencyId">The idempotency id associated with the commit.</param>
/// <param name="versionAttempted">The aggregate version that was attempted to be committed.</param>
/// <param name="version">The aggregate version present in the store.</param>
/// <param name="httpStatusMessage">The HTTP status message returned by the storage operation.</param>
public class CommitException(
	int errorCode,
	string aggregateId,
	string idempotencyId,
	int versionAttempted,
	int version,
	string httpStatusMessage
)
	: Exception(
		$"Failed to commit events.\n\tErrorCode: {errorCode} - {httpStatusMessage}\n\tAggregateId: {aggregateId}\n\tIdempotencyId: {idempotencyId}\n\tVersionAttempted: {versionAttempted}\n\tVersionPresent: {version}"
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the id of the aggregate.
	/// </summary>
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	/// <summary>
	/// Gets the aggregate version that was attempted to be committed.
	/// </summary>
	public int VersionAttempted { get; } = versionAttempted;

	/// <summary>
	/// Gets the idempotency id associated with the commit.
	/// </summary>
	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));

	/// <summary>
	/// Gets the aggregate version present in the store.
	/// </summary>
	public int Version { get; } = version;

	/// <summary>
	/// Gets the HTTP status message returned by the storage operation.
	/// </summary>
	public string HttpStatusMessage { get; } = httpStatusMessage;
}
