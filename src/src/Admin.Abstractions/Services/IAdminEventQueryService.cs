namespace Purview.EventSourcing.Admin.Abstractions;

public interface IAdminEventQueryService
{
	Task<PagedResult<EventEnvelopeResponse>?> GetRangeAsync(
		string aggregateType,
		string aggregateId,
		EventRangeQuery query,
		CancellationToken cancellationToken
	);
}
