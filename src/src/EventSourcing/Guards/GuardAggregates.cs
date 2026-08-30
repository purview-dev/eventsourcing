using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Guards;

/// <summary>
/// Guard helpers for validating aggregate state before mutating operations.
/// </summary>
/// <remarks>
/// These helpers are marked <see cref="EditorBrowsableAttribute(EditorBrowsableState)"/> as <see cref="EditorBrowsableState.Never"/>
/// so they do not pollute IntelliSense; they are intended for use from within aggregate command methods and providers.
/// </remarks>
[StackTraceHidden]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GuardAggregates
{
	/// <summary>
	/// Ensures the aggregate has not already been persisted, throwing an <see cref="ArgumentException"/> otherwise.
	/// </summary>
	/// <typeparam name="T">The aggregate type.</typeparam>
	/// <param name="value">The aggregate to validate.</param>
	/// <param name="errorMessage">Optional custom error message to use when the aggregate has already been saved.</param>
	/// <param name="paramName">The name of the parameter being validated, captured automatically from the caller.</param>
	/// <returns>The same <paramref name="value"/> instance if it has never been saved.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="value"/> has already been persisted (its
	/// <see cref="AggregateDetails.SavedVersion"/> is greater than zero).
	/// </exception>
	[return: NotNull]
	public static T MustBeNew<T>(
		[NotNull] this T value,
		string? errorMessage = null,
		[CallerMemberName] string? paramName = null
	)
		where T : class, IAggregate =>
		value.Details.SavedVersion > 0
			? throw new ArgumentException(
				errorMessage ?? $"The aggregate '{typeof(T).Name}' existing and cannot be modified in this way.",
				paramName
			)
			: value;
}
