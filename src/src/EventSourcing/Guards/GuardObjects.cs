using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Purview.EventSourcing.Guards;

/// <summary>
/// Guard helpers for validating object references.
/// </summary>
/// <remarks>
/// These helpers are marked <see cref="EditorBrowsableAttribute(EditorBrowsableState)"/> as <see cref="EditorBrowsableState.Never"/>
/// so they do not pollute IntelliSense; they are intended for use from within aggregate and provider code.
/// </remarks>
[StackTraceHidden]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GuardObjects
{
	/// <summary>
	/// Ensures the value is not null, throwing an <see cref="ArgumentNullException"/> otherwise.
	/// </summary>
	/// <typeparam name="T">The type of the value being validated.</typeparam>
	/// <param name="value">The value to validate.</param>
	/// <returns>The non-null <paramref name="value"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	[return: NotNullIfNotNull(nameof(value))]
	public static T Required<T>(this T? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		return value;
	}
}
