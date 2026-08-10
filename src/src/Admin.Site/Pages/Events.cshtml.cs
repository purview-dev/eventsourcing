using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Queries;
using Purview.EventSourcing.Admin.Abstractions.Services;

namespace Purview.EventSourcing.Admin.Site.Pages;

public class EventsModel(IAdminEventQueryService eventQueryService) : PageModel
{
	readonly IAdminEventQueryService _eventQueryService =
		eventQueryService ?? throw new ArgumentNullException(nameof(eventQueryService));

	[BindProperty(SupportsGet = true)]
	public string AggregateType { get; set; } = default!;

	[BindProperty(SupportsGet = true)]
	public string AggregateId { get; set; } = default!;

	[BindProperty(SupportsGet = true)]
	public long? VersionFrom { get; set; }

	[BindProperty(SupportsGet = true)]
	public long? VersionTo { get; set; }

	[BindProperty(SupportsGet = true)]
	public DateTime? TimeFromUtc { get; set; }

	[BindProperty(SupportsGet = true)]
	public DateTime? TimeToUtc { get; set; }

	[BindProperty(SupportsGet = true, Name = "pageNo")]
	public int PageNo { get; set; } = 1;

	[BindProperty(SupportsGet = true)]
	public int PageSize { get; set; } = 25;

	public PagedResult<EventEnvelopeResponse>? EventRange { get; set; }

	public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(AggregateType) || string.IsNullOrWhiteSpace(AggregateId))
		{
			return BadRequest("AggregateType and AggregateId are required.");
		}

		try
		{
			var query = new EventRangeQuery(
				VersionFrom,
				VersionTo,
				TimeFromUtc is not null ? new DateTimeOffset(TimeFromUtc.Value, TimeSpan.Zero) : null,
				TimeToUtc is not null ? new DateTimeOffset(TimeToUtc.Value, TimeSpan.Zero) : null,
				PageNo,
				PageSize,
				"Version asc"
			);

			EventRange = await _eventQueryService.GetRangeAsync(AggregateType, AggregateId, query, cancellationToken);
			return Page();
		}
		catch (InvalidOperationException ex)
		{
			ModelState.AddModelError(string.Empty, $"Failed to load events: {ex.Message}");
			return Page();
		}
		catch (ArgumentException ex)
		{
			ModelState.AddModelError(string.Empty, $"Failed to load events: {ex.Message}");
			return Page();
		}
	}
}
