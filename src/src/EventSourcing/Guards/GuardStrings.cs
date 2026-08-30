using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Purview.EventSourcing.Guards;

/// <summary>
/// Guard helpers for validating, normalizing, and combining string values.
/// </summary>
/// <remarks>
/// These helpers are marked <see cref="EditorBrowsableAttribute(EditorBrowsableState)"/> as <see cref="EditorBrowsableState.Never"/>
/// so they do not pollute IntelliSense; they are intended for use from within aggregate and provider code.
/// </remarks>
[StackTraceHidden]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GuardStrings
{
	const char DefaultKeySeparator = '_';

	/// <summary>
	/// Returns null when <paramref name="value"/> is null, empty, or whitespace; otherwise returns the value.
	/// </summary>
	/// <param name="value">The value to inspect.</param>
	/// <param name="trim">When true, the returned value is trimmed of surrounding whitespace.</param>
	/// <returns>Null when the value is blank, otherwise the (optionally trimmed) value.</returns>
	public static string? NullIfEmpty(this string? value, bool trim = true) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Required(trim: trim);

	/// <summary>
	/// Ensures the value is not null, empty, or whitespace, throwing an <see cref="ArgumentException"/> otherwise.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <param name="trim">When true, the returned value is trimmed of surrounding whitespace.</param>
	/// <param name="customExceptionMessage">Optional custom message to use for the thrown <see cref="ArgumentException"/>.</param>
	/// <param name="paramName">The name of the parameter being validated, captured automatically from the caller.</param>
	/// <returns>The validated, non-null value.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="value"/> is null, empty, or whitespace.
	/// </exception>
	[return: NotNull]
	public static string Required(
		this string? value,
		bool trim = true,
		string? customExceptionMessage = null,
		[CallerMemberName] string? paramName = null
	)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			if (customExceptionMessage != null)
				throw new ArgumentException(customExceptionMessage, paramName);

			ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
		}

		return trim ? value.TrimText() : value;
	}

	/// <summary>
	/// Trims the value of surrounding whitespace, returning null when the value is null.
	/// </summary>
	/// <param name="value">The value to trim.</param>
	/// <returns>The trimmed value, or null.</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? TrimText(this string? value) => value?.Trim();

	/// <summary>
	/// Converts the value to upper case using the invariant culture, returning null when the value is null.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>The upper-cased value, or null.</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? Upper(this string? value) => value?.ToUpperInvariant();

	/// <summary>
	/// Converts the value to lower case using the invariant culture, returning null when the value is null.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>The lower-cased value, or null.</returns>
	[return: NotNullIfNotNull(nameof(value))]
	[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
	public static string? Lower(this string? value) => value?.ToLowerInvariant();

	/// <summary>
	/// Trims the value and converts it to upper case, returning null when the value is null.
	/// </summary>
	/// <param name="value">The value to normalize.</param>
	/// <returns>The trimmed, upper-cased value, or null.</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? TrimTextUpper(this string? value) => value.TrimText().Upper();

	/// <summary>
	/// Trims the value and converts it to lower case, returning null when the value is null.
	/// </summary>
	/// <param name="value">The value to normalize.</param>
	/// <returns>The trimmed, lower-cased value, or null.</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? TrimTextLower(this string? value) => value.TrimText().Lower();

	/// <summary>
	/// Joins the provided keys into a single key string using the specified separator.
	/// </summary>
	/// <param name="keys">The keys to combine. Each key must be non-blank.</param>
	/// <param name="separator">The separator used to join the keys. Defaults to '_'.</param>
	/// <returns>The combined key string.</returns>
	/// <exception cref="ArgumentException">Thrown when no keys are provided.</exception>
	[return: NotNull]
	public static string Key(this IEnumerable<string> keys, char separator = DefaultKeySeparator)
	{
		keys = [.. (keys ?? []).Select(static key => key.Required(true))];

		return keys.Any()
			? string.Join(separator, keys)
			: throw new ArgumentException("At least one key must be provided.", nameof(keys));
	}

	/// <summary>
	/// Joins the provided keys into a single upper-cased key string using the specified separator.
	/// </summary>
	/// <param name="keys">The keys to combine. Each key must be non-blank.</param>
	/// <param name="separator">The separator used to join the keys. Defaults to '_'.</param>
	/// <returns>The combined, upper-cased key string.</returns>
	/// <exception cref="ArgumentException">Thrown when no keys are provided.</exception>
	[return: NotNull]
	public static string KeyUpper(this IEnumerable<string> keys, char separator = DefaultKeySeparator) =>
		Key(keys, separator).Upper();

	/// <summary>
	/// Joins the provided keys into a single lower-cased key string using the specified separator.
	/// </summary>
	/// <param name="keys">The keys to combine. Each key must be non-blank.</param>
	/// <param name="separator">The separator used to join the keys. Defaults to '_'.</param>
	/// <returns>The combined, lower-cased key string.</returns>
	/// <exception cref="ArgumentException">Thrown when no keys are provided.</exception>
	[return: NotNull]
	public static string KeyLower(this IEnumerable<string> keys, char separator = DefaultKeySeparator) =>
		Key(keys, separator).Lower();
}
