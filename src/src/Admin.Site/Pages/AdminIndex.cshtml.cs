using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Queries;
using Purview.EventSourcing.Admin.Abstractions.Services;

namespace Purview.EventSourcing.Admin.Site.Pages;

/// <summary>
/// Page model for the admin portal home page that searches for aggregates.
/// </summary>
/// <param name="aggregateQueryService">The query service used to search for aggregates.</param>
public class AdminIndexModel(IAdminAggregateQueryService aggregateQueryService) : PageModel
{
	readonly IAdminAggregateQueryService _aggregateQueryService =
		aggregateQueryService ?? throw new ArgumentNullException(nameof(aggregateQueryService));

	/// <summary>
	/// Gets or sets the aggregate type filter for the search.
	/// </summary>
	[BindProperty(SupportsGet = true)]
	public string? AggregateType { get; set; }

	/// <summary>
	/// Gets or sets the aggregate identifier filter for the search.
	/// </summary>
	[BindProperty(SupportsGet = true)]
	public string? AggregateId { get; set; }

	/// <summary>
	/// Gets or sets the one-based page number to display.
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "pageNo")]
	public int PageNo { get; set; } = 1;

	/// <summary>
	/// Gets or sets the number of results to show per page.
	/// </summary>
	[BindProperty(SupportsGet = true)]
	public int PageSize { get; set; } = 25;

	/// <summary>
	/// Gets or sets the search results to render on the page.
	/// </summary>
	public PagedResult<AggregateSummaryResponse>? SearchResults { get; set; }

	/// <summary>
	/// Handles GET requests for the page by executing an aggregate search.
	/// </summary>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>The page result.</returns>
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
				PageNo,
				PageSize,
				"LastUpdatedUtc desc"
			);

			SearchResults = await _aggregateQueryService.SearchAsync(query, cancellationToken);
			return Page();
		}
		catch (InvalidOperationException ex)
		{
			ModelState.AddModelError(string.Empty, $"Search failed: {ex.Message}");
			return Page();
		}
		catch (ArgumentException ex)
		{
			ModelState.AddModelError(string.Empty, $"Search failed: {ex.Message}");
			return Page();
		}
	}
}
