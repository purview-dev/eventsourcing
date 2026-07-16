using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

[StackTraceHidden]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GuardStrings
{
	const char DefaultKeySeparator = '_';

	public static string? NullIfEmpty(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

	[return: NotNull]
	public static string Required(this string? value, bool trim = true, [CallerMemberName] string? paramName = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

		return trim ? value.TrimText() : value!;
	}

	[return: NotNullIfNotNull(nameof(value))]
	public static string? TrimText(this string? value) => value?.Trim();

	[return: NotNullIfNotNull(nameof(value))]
	public static string? Upper(this string? value) => value?.ToUpperInvariant();

	[return: NotNullIfNotNull(nameof(value))]
	[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
	public static string? Lower(this string? value) => value?.ToLowerInvariant();

	[return: NotNullIfNotNull(nameof(value))]
	public static string? TrimTextUpper(this string? value) => value.TrimText().Upper();

	[return: NotNullIfNotNull(nameof(value))]
	public static string? TrimTextLower(this string? value) => value.TrimText().Lower();

	[return: NotNull]
	public static string Key(this IEnumerable<string> keys, char separator = DefaultKeySeparator)
	{
		keys = [.. (keys ?? []).Select(static key => key.Required(true))];

		return keys.Any()
			? string.Join(separator, keys)
			: throw new ArgumentException("At least one key must be provided.", nameof(keys));
	}

	[return: NotNull]
	public static string KeyUpper(this IEnumerable<string> keys, char separator = DefaultKeySeparator) =>
		Key(keys, separator).Upper();

	[return: NotNull]
	public static string KeyLower(this IEnumerable<string> keys, char separator = DefaultKeySeparator) =>
		Key(keys, separator).Lower();
}
