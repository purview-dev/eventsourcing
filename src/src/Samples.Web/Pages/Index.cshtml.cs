using Microsoft.AspNetCore.Mvc.RazorPages;
using Purview.EventSourcing.Samples;

namespace Purview.EventSourcing.Samples.Web.Pages;

sealed class IndexModel(SampleStoreOptions storeOptions) : PageModel
{
	public SampleStoreOptions StoreOptions { get; } = storeOptions;
}
