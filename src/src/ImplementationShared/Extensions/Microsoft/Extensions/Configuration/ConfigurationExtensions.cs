using System.ComponentModel;
using Purview.EventSourcing.Guards;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Configuration helpers for resolving connection strings, with required-value validation.
/// </summary>
/// <remarks>
/// Hidden from IntelliSense because these helpers are intended for provider-internal use rather than public
/// application API.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurationExtensions
{
	extension(IConfiguration configuration)
	{
		/// <summary>
		/// Gets the first connection string found for the supplied keys, throwing when none resolve.
		/// </summary>
		/// <param name="keys">The candidate connection-string keys to try, in order.</param>
		/// <returns>The resolved connection string.</returns>
		/// <exception cref="ArgumentException">Thrown when no connection string is found for any key.</exception>
		public string GetRequiredConnectionString(params string?[] keys)
		{
			keys = NotNull(keys);

			var result = GetConnectionString(configuration, keys);

			return result.Required(
				trim: true,
				customExceptionMessage: $"No connection string found for keys: {string.Join(", ", keys)}"
			);
		}

		/// <summary>
		/// Gets the first connection string found for the supplied keys, throwing when none resolve.
		/// </summary>
		/// <param name="keys">The candidate connection-string keys to try, in order.</param>
		/// <returns>The resolved connection string.</returns>
		/// <exception cref="ArgumentException">Thrown when no connection string is found for any key.</exception>
		public string GetRequiredConnectionString(IEnumerable<string?> keys) =>
			GetRequiredConnectionString(configuration, keys.ToArray());

		/// <summary>
		/// Gets the first non-blank connection string found for the supplied keys, or null when none resolve.
		/// </summary>
		/// <param name="keys">The candidate connection-string keys to try, in order.</param>
		/// <returns>The first matching connection string, or null.</returns>
		public string? GetConnectionString(string?[] keys) =>
			NotNull(keys)
				.Select(configuration.GetConnectionString)
				.FirstOrDefault(key => !string.IsNullOrWhiteSpace(key));

		static string[] NotNull(IEnumerable<string?> keys) =>
			keys?.Where(key => !string.IsNullOrWhiteSpace(key)).Cast<string>().ToArray() ?? [];
	}
}
