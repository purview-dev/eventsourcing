using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Guards;

[StackTraceHidden]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GuardAggregates
{
	[return: NotNull]
	public static T MustBeNew<T>(
		[NotNull] this T value,
		string? errorMessage = null,
		[CallerMemberName] string? paramName = null
	)
		where T : class, IAggregate =>
		value.Details.SavedVersion > 0
			? throw new ArgumentException(
				errorMessage
					?? $"The aggregate '{typeof(T).Name}' existing and cannot be modified in this way.",
				paramName
			)
			: value;
}
