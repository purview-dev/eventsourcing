namespace Purview.EventSourcing.ValueObjects;

/// <summary>
/// A value object whose creation requires additional aggregate context in order to be validated or constructed.
/// </summary>
/// <typeparam name="TSelf">The concrete value object type.</typeparam>
/// <typeparam name="TValue">The underlying value type wrapped by the value object.</typeparam>
/// <typeparam name="TAggregate">The aggregate type providing the creation context.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1005:Avoid excessive parameters on generic types")]
public interface IContextualValueObject<TSelf, TValue, TAggregate>
	where TSelf : IValueObject
{
	/// <summary>
	/// Creates a new instance using the supplied value and aggregate context.
	/// </summary>
	/// <param name="value">The value to wrap.</param>
	/// <param name="context">The aggregate and member context used for validation.</param>
	/// <returns>A new contextual value object instance.</returns>
	static abstract TSelf Create(TValue value, in ValueObjectContext<TAggregate> context);
}
