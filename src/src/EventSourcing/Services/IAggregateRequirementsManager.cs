using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Services;

/// <summary>
/// Fulfils the <see cref="IRequirement{T}"/> interfaces implemented by an aggregate by injecting the
/// required services.
/// </summary>
public interface IAggregateRequirementsManager
{
	/// <summary>
	/// Populates the aggregate's required service properties from the service provider.
	/// </summary>
	/// <param name="aggregate">The aggregate whose requirements should be fulfilled.</param>
	void Fulfil(IAggregate aggregate);
}
