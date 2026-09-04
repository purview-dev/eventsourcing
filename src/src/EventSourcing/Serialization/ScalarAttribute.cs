namespace Purview.EventSourcing.Serialization;

/// <summary>
/// Marks a struct or class as a scalar value object and controls how the source generator
/// produces conversion, comparison, and serialization members for it.
/// </summary>
/// <remarks>
/// <para>
/// A scalar value object wraps a single underlying primitive value. When the generator processes a type
/// decorated with this attribute, it can emit a JSON converter, <see cref="IComparable"/> support,
/// comparison operators, implicit conversions to and from the primitive, and an <c>Empty</c> instance.
/// </para>
/// <para>
/// The <see cref="PropertyName"/> identifies the member holding the underlying value. Scalar value objects are
/// serialized using only that member's value, which is what makes them query-friendly for primitive inner values.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class ScalarAttribute(string propertyName = "Value") : Attribute
{
	/// <summary>
	/// Gets the name of the property that holds the underlying scalar value.
	/// </summary>
	/// <value>Defaults to <c>Value</c> when not specified.</value>
	public string PropertyName { get; } = propertyName;

	/// <summary>
	/// Gets or sets whether a JSON converter should be generated for the value object.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateJsonConverter { get; init; } = true;

	/// <summary>
	/// Gets or sets whether the value object should implement <see cref="IComparable{T}"/>.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateComparable { get; init; } = true;

	/// <summary>
	/// Gets or sets whether comparison operators should be generated for the value object.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateComparisonOperators { get; init; } = true;

	/// <summary>
	/// Gets or sets whether enum properties should be generated from the underlying value.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateEnumProperties { get; init; } = true;

	/// <summary>
	/// Gets or sets whether an implicit conversion from the primitive value should be generated.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateImplicitFromPrimitive { get; init; } = true;

	/// <summary>
	/// Gets or sets whether an implicit conversion to the primitive value should be generated.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateImplicitToPrimitive { get; init; } = true;

	/// <summary>
	/// Gets or sets whether a static <c>Empty</c> instance should be generated.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateEmpty { get; init; } = true;

	/// <summary>
	/// Gets or sets the deserialization mode used by the generated JSON converter.
	/// </summary>
	/// <value>Defaults to <see cref="ValueObjectDeserializationMode.Hydrate"/>.</value>
	public ValueObjectDeserializationMode DeserializationMode { get; init; } = ValueObjectDeserializationMode.Hydrate;
}
