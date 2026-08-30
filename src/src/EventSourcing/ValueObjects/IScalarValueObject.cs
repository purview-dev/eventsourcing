namespace Purview.EventSourcing.ValueObjects;

/// <summary>
/// A value object that wraps a single underlying value of type <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TSelf">The concrete scalar value object type.</typeparam>
/// <typeparam name="TValue">The underlying value type wrapped by the value object.</typeparam>
/// <remarks>
/// Scalar value objects are persisted and serialized using only their <see cref="Value"/>, which is what makes
/// them query-friendly for primitive inner values. The static <see cref="Create(TValue)"/> and
/// <see cref="Hydrate(TValue)"/> factory methods are used by the generated conversion pipeline.
/// </remarks>
public interface IScalarValueObject<TSelf, TValue> : IValueObject, IComparable<TSelf>, IComparable
	where TSelf : IScalarValueObject<TSelf, TValue>
{
	/// <summary>
	/// Gets the underlying value wrapped by this value object.
	/// </summary>
	TValue Value { get; }

	/// <summary>
	/// Compares the underlying <see cref="Value"/> with another value.
	/// </summary>
	/// <param name="other">The value to compare against.</param>
	/// <returns>A value indicating the relative order of the compared values.</returns>
	int CompareTo(TValue other);

	/// <summary>
	/// Creates a new instance from a value, applying validation and normalization rules.
	/// </summary>
	/// <param name="value">The value to wrap.</param>
	/// <returns>A new scalar value object instance.</returns>
	/// <remarks>This is the canonical creation path used by callers and by the generated command pipeline.</remarks>
	static abstract TSelf Create(TValue value);

	/// <summary>
	/// Creates a new instance from a value without re-validating it.
	/// </summary>
	/// <param name="value">The value to wrap.</param>
	/// <returns>A new scalar value object instance.</returns>
	/// <remarks>This is used when reconstructing state from persisted data where validation has already occurred.</remarks>
	static abstract TSelf Hydrate(TValue value);
}
