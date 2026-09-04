using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos;

namespace Purview.EventSourcing.CosmosDb;

/// <summary>
/// Options used to configure the indexing policy of the Cosmos DB container.
/// </summary>
/// <remarks>
/// These options are applied when the container is created, and are reconciled against an existing
/// container so that missing paths and index settings are added without removing existing configuration.
/// </remarks>
public class CosmosDbIndexOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether indexing is automatically applied to newly written items.
	/// </summary>
	public bool Automatic { get; set; } = true;

	/// <summary>
	/// Gets or sets the indexing mode applied to the container.
	/// </summary>
	[EnumDataType(typeof(IndexingMode))]
	public IndexingMode IndexingModel { get; set; } = IndexingMode.Consistent;

	/// <summary>
	/// Gets or sets the paths that are explicitly included in the container's indexing policy.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1819:Properties should not return arrays",
		Justification = "DTO"
	)]
	public string[] IncludedPaths { get; set; } = [];

	/// <summary>
	/// Gets or sets the paths that are explicitly excluded from the container's indexing policy.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1819:Properties should not return arrays",
		Justification = "DTO"
	)]
	public string[] ExcludedPaths { get; set; } = [];

	/// <summary>
	/// Gets or sets the spatial paths included in the container's indexing policy.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1819:Properties should not return arrays",
		Justification = "DTO"
	)]
	public SpatialPath[] SpatialIndices { get; set; } = [];

	/// <summary>
	/// Gets or sets the composite index sets included in the container's indexing policy.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1819:Properties should not return arrays",
		Justification = "DTO"
	)]
	public CompositePath[][] CompositeIndices { get; set; } = [];

	/// <summary>
	/// Returns the hash code for this instance.
	/// </summary>
	/// <returns>A hash code for the current <see cref="CosmosDbIndexOptions"/>.</returns>
	public override int GetHashCode()
	{
		HashCode hashCode = new();

		hashCode.Add(IndexingModel);
		if (IncludedPaths != null)
			hashCode.Add(IncludedPaths);

		if (ExcludedPaths != null)
			hashCode.Add(ExcludedPaths);

		if (SpatialIndices != null)
			hashCode.Add(SpatialIndices);

		if (CompositeIndices != null)
			hashCode.Add(CompositeIndices);

		return hashCode.ToHashCode();
	}
}
