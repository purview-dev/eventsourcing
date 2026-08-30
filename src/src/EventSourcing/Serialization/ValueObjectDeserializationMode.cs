namespace Purview.EventSourcing.Serialization;

/// <summary>
/// Determines how value objects are reconstructed during JSON deserialization.
/// </summary>
public enum ValueObjectDeserializationMode
{
	/// <summary>
	/// Reconstructs the value object through its <c>Hydrate</c> factory without re-validating the value.
	/// </summary>
	/// <remarks>Used when reading persisted state where validation has already occurred.</remarks>
	Hydrate = 0,

	/// <summary>
	/// Reconstructs the value object through its <c>Create</c> factory, re-running validation rules.
	/// </summary>
	/// <remarks>Used when strict round-trip fidelity is required and validation must be enforced.</remarks>
	Strict = 1,
}
