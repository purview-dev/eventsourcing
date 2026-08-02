using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Purview.EventSourcing.Guards;

[StackTraceHidden]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GuardObjects
{
	[return: NotNullIfNotNull(nameof(value))]
	public static T Required<T>(this T? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		return value;
	}
}
