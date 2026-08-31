using System.Text.Json;

namespace Purview.EventSourcing.Admin.Site.Pages;

static class JsonFormatter
{
	static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

	public static string PrettyPrint(Purview.EventSourcing.Admin.Client.JsonElement element) =>
		JsonSerializer.Serialize(element.AdditionalProperties, Indented);
}
