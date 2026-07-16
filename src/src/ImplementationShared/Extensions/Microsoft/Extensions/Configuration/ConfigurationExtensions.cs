using System.ComponentModel;

namespace Microsoft.Extensions.Configuration;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurationExtensions
{
	extension(IConfiguration configuration)
	{
		public string GetRequiredConnectionString(params string?[] keys) =>
			GetConnectionString(configuration, keys).Required(trim: true);

		public string GetRequiredConnectionString(IEnumerable<string?> keys) =>
			GetConnectionString(configuration, keys?.ToArray()!).Required(trim: true);

		public string? GetConnectionString(string?[] keys) =>
			keys.Where(key => !string.IsNullOrWhiteSpace(key))
				.Select(x => configuration.GetConnectionString(x!))
				.FirstOrDefault(key => !string.IsNullOrWhiteSpace(key));
	}
}
