namespace Purview.EventSourcing.Admin.Abstractions;

public interface IAdminAggregateQueryService
{
	Task<PagedResult<AggregateSummaryResponse>> SearchAsync(
		AggregateSearchQuery query,
		CancellationToken cancellationToken);

	Task<AggregateSummaryResponse?> GetAsync(
		string aggregateType,
		string aggregateId,
		CancellationToken cancellationToken);
}
