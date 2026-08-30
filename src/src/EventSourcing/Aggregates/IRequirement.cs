namespace Purview.EventSourcing.Aggregates;

/// <summary>
/// Implemented by an aggregate to declare that it requires a service of type <typeparamref name="T"/>
/// to be injected before it is used.
/// </summary>
/// <typeparam name="T">The type of the service required by the aggregate.</typeparam>
/// <remarks>
/// The event store fulfils requirements automatically via <c>FulfilRequirements</c> when an aggregate is
/// created or loaded, resolving <typeparamref name="T"/> from the service provider and calling
/// <see cref="SetService"/>.
/// </remarks>
public interface IRequirement<T>
{
	/// <summary>
	/// Sets the required service instance on the aggregate.
	/// </summary>
	/// <param name="service">The resolved service instance.</param>
	void SetService(T service);
}
