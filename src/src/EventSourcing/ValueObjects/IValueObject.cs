namespace Purview.EventSourcing.ValueObjects;

/// <summary>
/// Marker interface identifying a type as a value object.
/// </summary>
/// <remarks>
/// Value objects are immutable types whose equality is based on the combination of their values rather than
/// reference identity. This marker is used by the source generator and serialization pipeline to apply
/// value-object-specific behaviors such as generated conversion and equality.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1040:Avoid empty interfaces",
	Justification = "Required to identify value objects in the domain model"
)]
public interface IValueObject { }

/// <summary>
/// A value object that supports typed comparison and implements <see cref="IValueObject"/>.
/// </summary>
/// <typeparam name="TSelf">The concrete value object type.</typeparam>
public interface IValueObject<TSelf> : IValueObject, IComparable<TSelf>, IComparable
	where TSelf : IValueObject<TSelf>
{
	//
}
