using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Services;

namespace Purview.EventSourcing.Admin.Site.Pages;

public class ProjectionModel(IAdminProjectionService projectionService) : PageModel
{
	readonly IAdminProjectionService _projectionService =
		projectionService ?? throw new ArgumentNullException(nameof(projectionService));

	[BindProperty(SupportsGet = true)]
	public required string AggregateType { get; set; }

	[BindProperty(SupportsGet = true)]
	public required string AggregateId { get; set; }

	[BindProperty(SupportsGet = true)]
	public long? Version { get; set; }

	[BindProperty(SupportsGet = true)]
	public DateTime? AsOfUtc { get; set; }

	public ProjectionResponse? Projection { get; set; }

	public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(AggregateType) || string.IsNullOrWhiteSpace(AggregateId))
		{
			return BadRequest("AggregateType and AggregateId are required.");
		}

		try
		{
			Projection =
				Version.HasValue && Version.Value > 0
					? await _projectionService.ProjectAtVersionAsync(
						AggregateType,
						AggregateId,
						Version.Value,
						cancellationToken
					)
				: AsOfUtc.HasValue
					? await _projectionService.ProjectAtTimeAsync(
						AggregateType,
						AggregateId,
						new DateTimeOffset(AsOfUtc.Value, TimeSpan.Zero),
						cancellationToken
					)
				: await _projectionService.ProjectAtVersionAsync(
					AggregateType,
					AggregateId,
					long.MaxValue,
					cancellationToken
				);

			return Page();
		}
		catch (InvalidOperationException ex)
		{
			ModelState.AddModelError(string.Empty, $"Failed to load projection: {ex.Message}");
			return Page();
		}
		catch (ArgumentException ex)
		{
			ModelState.AddModelError(string.Empty, $"Failed to load projection: {ex.Message}");
			return Page();
		}
	}
}
