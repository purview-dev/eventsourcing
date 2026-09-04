namespace Purview.EventSourcing.Serialization;

/// <summary>
/// Specifies assembly-level defaults for value object code generation.
/// These defaults can be overridden on individual <see cref="ValueObjectAttribute"/> attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ValueObjectDefaultsAttribute : Attribute
{
	/// <summary>
	/// Gets or sets whether parameterless constructors should be generated for value objects.
	/// When true, generates a private parameterless constructor for EF Core compatibility.
	/// Individual <see cref="ValueObjectAttribute"/> attributes can override this setting.
	/// Default: true
	/// </summary>
	public bool GenerateConstructor { get; init; } = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="ValueObjectDefaultsAttribute"/> class with default settings.
	/// </summary>
	public ValueObjectDefaultsAttribute() { }
}
