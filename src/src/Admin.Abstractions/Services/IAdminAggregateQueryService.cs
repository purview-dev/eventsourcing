using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Queries;

namespace Purview.EventSourcing.Admin.Abstractions.Services;

public interface IAdminAggregateQueryService
{
	Task<PagedResult<AggregateSummaryResponse>> SearchAsync(
		AggregateSearchQuery query,
		CancellationToken cancellationToken
	);

	Task<AggregateSummaryResponse?> GetAsync(
		string aggregateType,
		string aggregateId,
		CancellationToken cancellationToken
	);
}
