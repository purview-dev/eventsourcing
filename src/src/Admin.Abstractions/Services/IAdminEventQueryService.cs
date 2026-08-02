using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Queries;

namespace Purview.EventSourcing.Admin.Abstractions.Services;

public interface IAdminEventQueryService
{
	Task<PagedResult<EventEnvelopeResponse>?> GetRangeAsync(
		string aggregateType,
		string aggregateId,
		EventRangeQuery query,
		CancellationToken cancellationToken
	);
}
