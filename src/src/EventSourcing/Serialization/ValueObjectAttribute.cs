namespace Purview.EventSourcing.Serialization;

/// <summary>
/// Marks a struct or class as a complex (non-scalar) value object and controls how the source generator
/// produces conversion, comparison, and serialization members for it.
/// </summary>
/// <remarks>
/// A complex value object wraps multiple members. When the generator processes a type decorated with this
/// attribute, it can emit a JSON converter, <see cref="IComparable"/> support, comparison operators, an
/// <c>Empty</c> instance, and a constructor. See also <see cref="ScalarAttribute"/> for single-value wrappers
/// and <see cref="ValueObjectDefaultsAttribute"/> for assembly-level defaults.
/// </remarks>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class ValueObjectAttribute : Attribute
{
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
	/// Gets or sets whether a static <c>Empty</c> instance should be generated.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateEmpty { get; init; } = true;

	/// <summary>
	/// Gets or sets whether a constructor should be generated for the value object.
	/// </summary>
	/// <value>Defaults to <see langword="true"/>.</value>
	public bool GenerateConstructor { get; init; } = true;

	/// <summary>
	/// Gets or sets the deserialization mode used by the generated JSON converter.
	/// </summary>
	/// <value>Defaults to <see cref="ValueObjectDeserializationMode.Hydrate"/>.</value>
	public ValueObjectDeserializationMode DeserializationMode { get; init; } = ValueObjectDeserializationMode.Hydrate;
}
