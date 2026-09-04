using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.AzureStorage;

partial class TableEventStore<T>
{
	///<inheritdoc/>
	[DebuggerStepThrough]
	public Task<SaveResult<T>> SaveAsync(
		[NotNull] T aggregate,
		EventStoreOperationContext? operationContext,
		CancellationToken cancellationToken = default
	) => _saveOperation.SaveAsync(aggregate, operationContext, cancellationToken);
}
