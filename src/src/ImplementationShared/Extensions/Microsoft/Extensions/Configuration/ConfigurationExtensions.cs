using System.ComponentModel;
using Purview.EventSourcing.Guards;

namespace Microsoft.Extensions.Configuration;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurationExtensions
{
	extension(IConfiguration configuration)
	{
		public string GetRequiredConnectionString(params string?[] keys)
		{
			keys = NotNull(keys);

			var result = GetConnectionString(configuration, keys);

			return result.Required(
				trim: true,
				customExceptionMessage: $"No connection string found for keys: {string.Join(", ", keys)}"
			);
		}

		public string GetRequiredConnectionString(IEnumerable<string?> keys) =>
			GetRequiredConnectionString(configuration, keys?.ToArray()!);

		public string? GetConnectionString(string?[] keys) =>
			NotNull(keys)
				.Select(x => configuration.GetConnectionString(x!))
				.FirstOrDefault(key => !string.IsNullOrWhiteSpace(key));

		static string[] NotNull(IEnumerable<string?> keys) =>
			keys?.Where(key => !string.IsNullOrWhiteSpace(key)).Cast<string>().ToArray() ?? [];
	}
}
