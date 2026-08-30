using Azure.Data.Tables;

namespace Purview.EventSourcing.AzureStorage.StorageClients.Table;

#pragma warning disable CA1032 // Implement standard exception constructors
/// <summary>
/// Thrown when an Azure Table Storage operation fails with an error status code.
/// </summary>
/// <param name="entity">The entity the failed operation was performed on.</param>
/// <param name="actionType">The table transaction action that failed.</param>
/// <param name="response">The <see cref="Azure.Response"/> returned by the failed operation.</param>
public sealed class TableOperationException(
	ITableEntity entity,
	TableTransactionActionType actionType,
	Azure.Response response
)
	: Exception(
		$"Operation {actionType} failed with status {response.Status}.\n\tPartition Key: {entity.PartitionKey}\n\tRow Key: {entity.RowKey}"
	)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	/// <summary>
	/// Gets the entity the failed operation was performed on.
	/// </summary>
	public ITableEntity Entity { get; set; } = entity;

	/// <summary>
	/// Gets the table transaction action that failed.
	/// </summary>
	public TableTransactionActionType ActionType { get; set; } = actionType;

	/// <summary>
	/// Gets the <see cref="Azure.Response"/> returned by the failed operation.
	/// </summary>
	public Azure.Response Response { get; set; } = response;
}
