using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Purview.EventSourcing.Admin.Abstractions;

namespace Purview.EventSourcing.Admin.Site.Pages;

public class EventsModel : PageModel
{
	private readonly IAdminEventQueryService _eventQueryService;

	public EventsModel(IAdminEventQueryService eventQueryService)
	{
		_eventQueryService = eventQueryService ?? throw new ArgumentNullException(nameof(eventQueryService));
	}

	[BindProperty(SupportsGet = true)]
	public required string AggregateType { get; set; }

	[BindProperty(SupportsGet = true)]
	public required string AggregateId { get; set; }

	[BindProperty(SupportsGet = true)]
	public long? VersionFrom { get; set; }

	[BindProperty(SupportsGet = true)]
	public long? VersionTo { get; set; }

	[BindProperty(SupportsGet = true)]
	public DateTime? TimeFromUtc { get; set; }

	[BindProperty(SupportsGet = true)]
	public DateTime? TimeToUtc { get; set; }

	[BindProperty(SupportsGet = true)]
	public int Page { get; set; } = 1;

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
				Page,
				PageSize,
				"Version asc"
			);

			EventRange = await _eventQueryService.GetRangeAsync(AggregateType, AggregateId, query, cancellationToken);
			return Page();
		}
		catch (Exception ex)
		{
			ModelState.AddModelError(string.Empty, $"Failed to load events: {ex.Message}");
			return Page();
		}
	}
}
