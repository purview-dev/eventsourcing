namespace Purview.EventSourcing.AzureStorage.StorageClients.Table;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when entities with different partition keys are added to the same batch operation.
/// </summary>
/// <param name="existingPartitionKey">The partition key already present in the batch.</param>
/// <param name="invalidPartitionKey">The partition key that does not match the batch.</param>
public sealed class InvalidPartitionKeyException(string existingPartitionKey, string invalidPartitionKey)
	: Exception(
		$"Batched entities must have matching partition keys.\n\nExpected: {existingPartitionKey}\nPassed: {invalidPartitionKey}"
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the partition key that does not match the batch.
	/// </summary>
	public string InvalidPartitionKey { get; } = invalidPartitionKey;

	/// <summary>
	/// Gets the partition key already present in the batch.
	/// </summary>
	public string ExistingPartitionKey { get; } = existingPartitionKey;
}
