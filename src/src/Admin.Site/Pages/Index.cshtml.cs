using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Purview.EventSourcing.Admin.Abstractions;

namespace Purview.EventSourcing.Admin.Site.Pages;

public class IndexModel : PageModel
{
	private readonly IAdminAggregateQueryService _aggregateQueryService;

	public IndexModel(IAdminAggregateQueryService aggregateQueryService)
	{
		_aggregateQueryService =
			aggregateQueryService ?? throw new ArgumentNullException(nameof(aggregateQueryService));
	}

	[BindProperty(SupportsGet = true)]
	public string? AggregateType { get; set; }

	[BindProperty(SupportsGet = true)]
	public string? AggregateId { get; set; }

	[BindProperty(SupportsGet = true)]
	public int Page { get; set; } = 1;

	[BindProperty(SupportsGet = true)]
	public int PageSize { get; set; } = 25;

	public PagedResult<AggregateSummaryResponse>? SearchResults { get; set; }

	public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
	{
		try
		{
			var query = new AggregateSearchQuery(
				AggregateType,
				AggregateId,
				null,
				null,
				null,
				null,
				Page,
				PageSize,
				"LastUpdatedUtc desc"
			);

			SearchResults = await _aggregateQueryService.SearchAsync(query, cancellationToken);
			return Page();
		}
		catch (Exception ex)
		{
			ModelState.AddModelError(string.Empty, $"Search failed: {ex.Message}");
			return Page();
		}
	}
}
