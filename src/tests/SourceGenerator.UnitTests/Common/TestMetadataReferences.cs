using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Common;

public static class TestMetadataReferences
{
	public static IReadOnlyList<MetadataReference> Create()
	{
		var references = new List<MetadataReference>
		{
			// Without this, all of the references to event sourcing types will fail.
			MetadataReference.CreateFromFile(typeof(Aggregates.IAggregate).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
			MetadataReference.CreateFromFile(
				System.Reflection.Assembly.Load("System.Runtime").Location
			),
			MetadataReference.CreateFromFile(
				typeof(System.Text.Json.JsonSerializer).Assembly.Location
			),
			MetadataReference.CreateFromFile(
				typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location
			),
		};

		// Add netstandard reference
		var netstandard = System.Reflection.Assembly.Load(
			"netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"
		);
		references.Add(MetadataReference.CreateFromFile(netstandard.Location));

		return references;
	}
}
