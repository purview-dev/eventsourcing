using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Purview.EventSourcing.Admin.Abstractions;

namespace Purview.EventSourcing.Admin.Site.Pages;

public class ProjectionModel : PageModel
{
	private readonly IAdminProjectionService _projectionService;

	public ProjectionModel(IAdminProjectionService projectionService)
	{
		_projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
	}

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
			if (Version.HasValue && Version.Value > 0)
			{
				Projection = await _projectionService.ProjectAtVersionAsync(
					AggregateType,
					AggregateId,
					Version.Value,
					cancellationToken
				);
			}
			else if (AsOfUtc.HasValue)
			{
				Projection = await _projectionService.ProjectAtTimeAsync(
					AggregateType,
					AggregateId,
					new DateTimeOffset(AsOfUtc.Value, TimeSpan.Zero),
					cancellationToken
				);
			}
			else
			{
				// Default to latest version
				Projection = await _projectionService.ProjectAtVersionAsync(
					AggregateType,
					AggregateId,
					long.MaxValue,
					cancellationToken
				);
			}

			return Page();
		}
		catch (Exception ex)
		{
			ModelState.AddModelError(string.Empty, $"Failed to load projection: {ex.Message}");
			return Page();
		}
	}
}
