using Purview.EventSourcing.Admin.Abstractions.Models;

namespace Purview.EventSourcing.Admin.Abstractions.Services;

public interface IAdminProjectionService
{
	Task<ProjectionResponse?> ProjectAtVersionAsync(
		string aggregateType,
		string aggregateId,
		long targetVersion,
		CancellationToken cancellationToken
	);

	Task<ProjectionResponse?> ProjectAtTimeAsync(
		string aggregateType,
		string aggregateId,
		DateTimeOffset targetUtc,
		CancellationToken cancellationToken
	);
}
